﻿using System;
using Atomex.Core.Entities;
using Atomex.Wallet.Abstract;
using Atomex.Wallet.BitcoinBased;
using Atomex.Wallet.Ethereum;
using Atomex.Wallet.Tezos;

namespace Atomex.Wallet
{
    public static class CurrencyAccountCreator
    {
        public static ICurrencyAccount Create(
            Currency currency,
            IHdWallet wallet,
            IAccountDataRepository dataRepository,
            IAssetWarrantyManager assetWarrantyManager)
        {
            switch (currency)
            {
                case BitcoinBasedCurrency _:
                    return new BitcoinBasedAccount(
                        currency,
                        wallet,
                        dataRepository,
                        assetWarrantyManager);
                case Atomex.Ethereum _:
                    return new EthereumAccount(
                        currency,
                        wallet,
                        dataRepository,
                        assetWarrantyManager);
                case Atomex.Tezos _:
                    return new TezosAccount(
                        currency,
                        wallet,
                        dataRepository,
                        assetWarrantyManager);
                default:
                    throw new NotSupportedException($"Not supported currency {currency.Name}");
            }
        }
    }
}