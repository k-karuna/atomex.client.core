﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Blockchain.Tezos;
using Atomex.Common;
using Atomex.Core;
using Serilog;

namespace Atomex.Swaps.Tezos.FA12.Helpers
{
    public static class FA12SwapRefundedHelper
    {
        public static async Task<Result<bool>> IsRefundedAsync(
            Swap swap,
            Currency currency,
            Atomex.Tezos tezos,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Log.Debug("Tezos: check refund event");

                var fa12 = (TezosTokens.FA12)currency;

                var contractAddress = fa12.SwapContractAddress;

                var blockchainApi = (ITezosBlockchainApi)tezos.BlockchainApi;

                var txsResult = await blockchainApi
                    .TryGetTransactionsAsync(contractAddress, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (txsResult == null)
                    return new Error(Errors.RequestError, $"Connection error while getting {contractAddress} transactions");

                if (txsResult.HasError)
                {
                    Log.Error("Error while get transactions from contract {@contract}. Code: {@code}. Description: {@desc}",
                        contractAddress,
                        txsResult.Error.Code,
                        txsResult.Error.Description);

                    return txsResult.Error;
                }

                var txs = txsResult.Value
                    ?.Cast<TezosTransaction>()
                    .ToList();

                if (txs != null)
                {
                    foreach (var tx in txs)
                    {
                        if (tx.To == contractAddress && IsSwapRefund(tx, swap.SecretHash))
                            return true;

                        if (tx.BlockInfo?.BlockTime == null)
                            continue;

                        var blockTimeUtc = tx.BlockInfo.BlockTime.Value.ToUniversalTime();
                        var swapTimeUtc = swap.TimeStamp.ToUniversalTime();

                        if (blockTimeUtc < swapTimeUtc)
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Tezos refund control task error");

                return new Error(Errors.InternalError, e.Message);
            }

            return false;
        }

        public static async Task<Result<bool>> IsRefundedAsync(
            Swap swap,
            Currency currency,
            Atomex.Tezos tezos,
            int attempts,
            int attemptIntervalInSec,
            CancellationToken cancellationToken = default)
        {
            var attempt = 0;

            while (!cancellationToken.IsCancellationRequested && attempt < attempts)
            {
                ++attempt;

                var isRefundedResult = await IsRefundedAsync(
                        swap: swap,
                        currency: currency,
                        tezos: tezos,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (isRefundedResult.HasError && isRefundedResult.Error.Code != Errors.RequestError) // has error
                    return isRefundedResult;

                if (!isRefundedResult.HasError)
                    return isRefundedResult;

                await Task.Delay(TimeSpan.FromSeconds(attemptIntervalInSec), cancellationToken)
                    .ConfigureAwait(false);
            }

            return new Error(Errors.MaxAttemptsCountReached, "Max attempts count reached for refund check");
        }

        public static bool IsSwapRefund(TezosTransaction tx, byte[] secretHash)
        {
            try
            {
                var secretHashBytes = Hex.FromString(tx.Params["value"]["bytes"].ToString());

                return secretHashBytes.SequenceEqual(secretHash);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}