using NinjaTrader.Cbi;
using System;
using System.Collections.Generic;
using NinjaTrader.Custom.AddOns.Aurora.SDK.Block;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK.Engines
{
    public sealed class ExecutionEngine
    {
        private readonly HostStrategy _host;

        public ExecutionEngine(HostStrategy host)
        {
            //_host = Guard.NotNull(host, nameof(host));
        }

        public Order Execute(SignalContext Cx0)
        {
            try
            {
                if (Cx0.isEntry) // Entries
                {
                    switch (Cx0.Type)
                    {
                        case SignalOrderTypes.Market:
                            if (Cx0.Direction == MarketPosition.Long) _host.EnterLong(Cx0.Size, Cx0.Name);
                            else if (Cx0.Direction == MarketPosition.Short) _host.EnterShort(Cx0.Size, Cx0.Name);
                            break;

                        case SignalOrderTypes.StopMarket:
                            if (Cx0.Direction == MarketPosition.Long) _host.EnterLongStopMarket(Cx0.Size, Cx0.StopPrice, Cx0.Name);
                            else if (Cx0.Direction == MarketPosition.Short) _host.EnterShortStopMarket(Cx0.Size, Cx0.StopPrice, Cx0.Name);
                            break;
                    }
                }
                else // Exits
                {
                    switch (Cx0.Type)
                    {
                        case SignalOrderTypes.Market:
                            if (Cx0.Direction == MarketPosition.Long) _host.ExitLong(Cx0.Size);
                            else if (Cx0.Direction == MarketPosition.Short) _host.ExitShort(Cx0.Size);
                            break;

                        case SignalOrderTypes.StopMarket:
                            if (Cx0.Direction == MarketPosition.Long) _host.ExitLongStopMarket(Cx0.StopPrice);
                            else if (Cx0.Direction == MarketPosition.Short) _host.ExitShortStopMarket(Cx0.StopPrice);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                //_host.ATDebug($"ExecutionEngine: Execute() exception: {ex}", LogMode.Log, LogLevel.Error);
                return null;
            }

            return null;
        }
    }
}
