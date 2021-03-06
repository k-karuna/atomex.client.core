﻿using System.Collections.Generic;

namespace Atomex.MarketData.Abstract
{
    public interface IMarketDataRepository
    {
        void ApplyQuotes(IList<Quote> quotes);
        void ApplyEntries(IList<Entry> entries);
        void ApplySnapshot(Snapshot snapshot);
        MarketDataOrderBook OrderBookBySymbol(string symbol);
        Quote QuoteBySymbol(string symbol);
    }
}