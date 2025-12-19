
using NinjaTrader.Cbi;
using NinjaTrader.Custom.AddOns.Aurora.SDK.Block;
using NinjaTrader.Custom.AddOns.Aurora.SDK.Engines;
using System.Collections.Generic;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK
{
    public partial class LayerStrategy
    {
        private HostStrategy _host;
        private List<LogicBlock> _blocks;

        private EntryHandler _entryHandler;
        private ExitHandler _exitHandler;

        // Gets called in State.DataLoaded from OnStateChange in HostStrategy
        public void Initialize(HostStrategy host, List<LogicBlock> lbs)
        {
            _host = host;
            
            // TODO: do more shit up in here
            // Parse logic blocks
        }

        public struct SignalContext
        {
            public bool isEntry;
            public SignalOrderTypes Type;
            public MarketPosition Direction;
            public int Size;
            public string Name;
            public double LimitPrice;
            public double StopPrice;
        }

        // Gets called from OnBarUpdate in HostStrategy
        public SignalContext Forward()
        {
            EntryHandler.EntryProduct SGL1 = _entryHandler.Evaluate();
            ExitHandler.ExitProduct RSK1 = _exitHandler.Evaluate();

            // TODO: spit out an execution signal

            return new();
        }
    }
}
