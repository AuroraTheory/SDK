using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
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
    public abstract class AuroraStrategy : Strategy
    {
        // TODO: Create a time series data structure to hold external data sources
        // TODO: Then create a dict or list to hold those datastructures to be called by logic blocks
        #region Base Risk Parameters
        [NinjaScriptProperty, Range(1, int.MaxValue), Display(Name = "Base Contracts", Order = 1, GroupName = "RE_BASE")]
        public int Risk_BaseContracts { get; set; }

        [NinjaScriptProperty, Range(1, int.MaxValue), Display(Name = "Max Contracts", Order = 2, GroupName = "RE_BASE")]
        public int Risk_MaxContracts { get; set; }
        #endregion

        private SignalEngine _signalEngine;
        private RiskEngine _riskEngine;
        private UpdateEngine _updateEngine;
        private ExecutionEngine _executionEngine;

        private List<LogicBlock> Blocks;

        public void SetLogicBlocks(List<LogicBlock> blocks)
        {
            Blocks = blocks;
        }

        private void SetDefaultsHandler()
        {
            Risk_BaseContracts = 10;
            Risk_MaxContracts = int.MaxValue;
        }

        private void ConfigureHandler()
        {
            // Configuration logic can be added here
        }

        private void DataLoadedHandler()
        {
            List<LogicBlock> _sBlocks = ParseLogicBlocks(Blocks, BlockTypes.Signal);
            List<LogicBlock> _rBlocks = ParseLogicBlocks(Blocks, BlockTypes.Risk);
            List<LogicBlock> _uBlocks = ParseLogicBlocks(Blocks, BlockTypes.Update);
            List<LogicBlock> _eBlocks = ParseLogicBlocks(Blocks, BlockTypes.Execution);

            _signalEngine = new(this, _sBlocks);
            _riskEngine = new(this, _rBlocks, Risk_BaseContracts);
            _updateEngine = new(this, _uBlocks);
            _executionEngine = new(this, _eBlocks);
        }

        public void OnStateChangedHandler(State state)
        {
            switch (state)
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

        protected override void OnBarUpdate()
        {
            Print("Aurora OnBarUpdate Triggered");
            if (_signalEngine == null || _riskEngine == null || _updateEngine == null || _executionEngine == null)
            {
                Print("Aurora Engines not initialized.");
                return;
            }
            SignalEngine.SignalProduct SGL1 = _signalEngine.Evaluate();
            RiskEngine.RiskProduct RSK1 = _riskEngine.Evaluate();

            _updateEngine.Update(UpdateEngine.UpdateTypes.OnBarUpdate);
            ExecutionEngine.ExecutionProduct EXC1 = _executionEngine.Execute(SGL1, RSK1);
        }
    }
}
