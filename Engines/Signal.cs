using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK.Engines
{
    public sealed class SignalEngine
    {
        public struct SignalProduct
        {
            public OrderType OrderType;
            public MarketPosition Direction;
            public string Name;

            public static SignalProduct Flat(string name)
            {
                return new SignalProduct
                {
                    OrderType = OrderType.Market,
                    Direction = MarketPosition.Flat,
                    Name = name ?? string.Empty
                };
            }
        }

        public SignalEngine(HostStrategy host, List<LogicBlock> logicBlocks)
        {
            
        }

        public SignalProduct Evaluate()
        {
            return new();
        }
    }
}
