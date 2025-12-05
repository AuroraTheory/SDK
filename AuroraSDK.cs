using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using SharpDX.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK
{
    public class AuroraStrategy
    {
        // this is the top level class for Aurora Strategies,
        internal StrategyBase _Host;

        internal List<ISeries<double>> _Primaries;
        // TODO: Create a time series data structure to hold external data sources
        // TODO: Then create a dict or list to hold those datastructures to be called by logic blocks

        public SignalEngine _signalEngine { get; private set; }
        public RiskEngine _riskEngine { get; private set; }
        public UpdateEngine _updateEngine { get; private set; }
        public ExecutionEngine _executionEngine { get; private set; }


        public AuroraStrategy(StrategyBase Host, List<LogicBlock> Blocks) // this would be called during DataLoaded
        {
            _Host = Host;
            _Primaries = LoadPrimarySeries();

            List<LogicBlock> _sBlocks = ParseLogicBlocks(Blocks, BlockTypes.Signal);
            List<LogicBlock> _rBlocks = ParseLogicBlocks(Blocks, BlockTypes.Risk);
            List<LogicBlock> _uBlocks = ParseLogicBlocks(Blocks, BlockTypes.Update);
            //List<LogicBlock> _eBlocks = ParseLogicBlocks(Blocks, BlockTypes.Signal);

            _signalEngine = new(Host, _sBlocks);
            _riskEngine = new(Host, _rBlocks);
            _updateEngine = new(Host, _uBlocks);
            //_executionEngine = new();
        }

        public List<ISeries<double>> LoadPrimarySeries()
        {
            List<ISeries<double>> primaries = [];
            primaries.Clear();

            foreach (var barSeries in _Host.BarsArray)
                _Primaries.Add(barSeries);

            return primaries;
        }

        public List<LogicBlock> ParseLogicBlocks(List<LogicBlock> blocks, BlockTypes type)
        {
            List<LogicBlock> parsedBlocks = [];
            foreach (LogicBlock block in blocks)
            {
                if (block.Type == type)
                    parsedBlocks.Add(block);
            }
            return parsedBlocks;
        }

        public void BarUpdate()
        {
            SignalEngine.SignalProduct SGL1 = _signalEngine.Evaluate(); // should we have a global state datastructure?
            RiskEngine.RiskProduct RSK1 = _riskEngine.Evaluate(SGL1); // TODO: Risk Engine currently does NOT use SignalProduct, need to implement that

            _updateEngine.Update(UpdateEngine.UpdateTypes.OnBarUpdate);
            ExecutionEngine.ExecutionProduct EXC1 = _executionEngine.Execute(SGL1, RSK1);
        }
    }
}
