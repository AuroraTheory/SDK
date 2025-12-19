using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System.Collections.Generic;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK
{
    // Host Strategy Abstract Class
    // Only Takes Entry and Signal Signals from LayerStrategy
    public abstract partial class HostStrategy : Strategy
    {
        private LayerStrategy _layer;
        private Engines.ExecutionEngine _eEngine;
        private List<Block.LogicBlock> _metaBlocks;

        protected abstract bool Register();

        // Initialization Point
        protected override void OnStateChange()
        {
            switch (State)
            {
                case State.SetDefaults:
                    SetDefaultsHandler();
                    break;
                case State.Configure:
                    ConfigureHandler();
                    break;
                case State.DataLoaded:
                    DataLoadedHandler();
                    break;
            }
        }

        // Main Entry Point
        protected override void OnBarUpdate()
        {
            LayerStrategy.SignalContext Cx0 = _layer.Forward();
            List<Block.LogicBlock.LogicTicket> Lts = [];

            if (_metaBlocks != null && _metaBlocks.Count != 0)
                foreach (Block.LogicBlock lb in _metaBlocks)
                {
                    Block.LogicBlock.LogicTicket lt0 = lb.SafeGuardForward([]); // single value: bool
                    Lts.Add(lt0);
                    if ((bool)lt0.Values[0] == true)
                        return;
                }
            else return;

            _eEngine.Execute(Cx0);
            return;
        }
    }
}
