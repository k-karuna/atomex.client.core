﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Atomex.Common;

namespace Atomex.Blockchain.Ethereum
{
    public class EthereumNonceManager
    {
        private TimeSpan ExpirationTimeOut { get; } = TimeSpan.FromSeconds(180);

        private class NonceEntry
        {
            public BigInteger Value { get; set; }
            public DateTime LastUpdatedTimeUtc { get; set; }
        }

        private readonly IDictionary<string, NonceEntry> _nonces;
        private readonly object _syncRoot;

        private EthereumNonceManager()
        {
            _nonces = new Dictionary<string, NonceEntry>();
            _syncRoot = new object();
        }

        private static EthereumNonceManager _instance;
        public static EthereumNonceManager Instance => _instance ?? (_instance = new EthereumNonceManager());

        public async Task<Result<BigInteger>> GetNonceAsync(
            Atomex.Ethereum ethereum,
            string address,
            CancellationToken cancellationToken = default)
        {
            var transactionCountResult = await ((IEthereumBlockchainApi)ethereum.BlockchainApi)
                .GetTransactionCountAsync(address, cancellationToken)
                .ConfigureAwait(false);

            if (transactionCountResult.HasError)
                return transactionCountResult;

            var nonce = transactionCountResult.Value;

            lock (_syncRoot)
            {
                if (_nonces.TryGetValue(address, out var offlineNonce))
                {
                    if (offlineNonce.Value >= nonce &&
                        DateTime.UtcNow - offlineNonce.LastUpdatedTimeUtc <= ExpirationTimeOut)
                    {
                        return offlineNonce.Value++;
                    }

                    _nonces[address] = new NonceEntry
                    {
                        Value = nonce + 1,
                        LastUpdatedTimeUtc = DateTime.UtcNow
                    };

                    return nonce;
                }

                _nonces.Add(address, new NonceEntry
                {
                    Value = nonce + 1,
                    LastUpdatedTimeUtc = DateTime.UtcNow
                });

                return nonce;
            }
        }
    }
}