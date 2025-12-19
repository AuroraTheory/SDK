
using System.Collections.Generic;
using System.Windows.Documents;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK
{
    public partial class LayerStrategy
    {
        private HostStrategy _host;
        private List<LogicBlock> _blocks;

        private Engines.SignalEngine _signalEngine;
        private Engines.RiskEngine _riskEngine;

        // Gets called from OnStateChange(DataLoaded)
        public void Initialize(HostStrategy host, List<LogicBlock> lbs)
        {
            _host = host;
    
        }

        // Gets called from OnBarUpdate
        public SignalContext Forward()
        {
            // Loop through L0 Logic Blocks
            Engines.SignalEngine.SignalProduct SGL1 = _signalEngine.Evaluate();
            Engines.RiskEngine.RiskProduct RSK1 = _riskEngine.Evaluate();

            // spit out an execution signal
            return new();
        }
    }
}
