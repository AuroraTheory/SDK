using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK.Engines
{
    public sealed class EntryHandler
    {
        public struct EntryProduct
        {
            public OrderType OrderType;
            public MarketPosition Direction;
            public string Name;

            public static EntryProduct Flat(string name)
            {
                return new EntryProduct
                {
                    OrderType = OrderType.Market,
                    Direction = MarketPosition.Flat,
                    Name = name ?? string.Empty
                };
            }
        }

        public EntryHandler(HostStrategy host, List<LogicBlock> logicBlocks)
        {
            
        }

        public EntryProduct Evaluate()
        {
            return new();
        }
    }
}
