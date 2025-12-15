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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static NinjaTrader.NinjaScript.Strategies.LeafSDK;

namespace NinjaTrader.Custom.Strategies.Aurora.SDK
{
    public abstract partial class AuroraStrategy : Strategy
    {
        // TODO: Create a time series data structure to hold external data sources
        // TODO: Then create a dict or list to hold those datastructures to be called by logic blocks
        [NinjaScriptProperty, Display(Name = "GLOBAL DEBUG MODE", GroupName = "Aurora Settings")]
        public bool DEBUG { get; set; } = false;

        [NinjaScriptProperty, Display(Name = "CONFIG FILE", GroupName = "Aurora Settings")]
        public string CFGPATH { get; set; } = "";

        private SignalEngine _signalEngine;
        private RiskEngine _riskEngine;
        private UpdateEngine _updateEngine;
        private ExecutionEngine _executionEngine;

        public enum LogMode
        {
            Log,
            Print,
            Debug
        }

        protected abstract void Register();

        public List<LogicBlock> ParseConfigFile(string filePath)
        {
            AlgoConfig algoConfig = new();
            LogicBlockFactory blockFactory = new();
            List<LogicBlock> lbs = new();
            try
            {
                algoConfig = YamlRawLoader.Load(filePath);
                if (algoConfig == null || algoConfig.Logic == null || algoConfig.Logic.Count == 0)
                {
                    this.ATDebug($"ParseConfigFile: Config is Null", LogMode.Log, LogLevel.Error);
                    throw new NullReferenceException();
                }
                foreach (LogicBlockConfig lbc in algoConfig.Logic)
                {
                    LogicBlock lb = blockFactory.Create(lbc.BID, this, lbc.BParameters);
                    lbs.Add(lb);
                }
                ATDebug("ALL BLOCKS INITIALIZED", LogMode.Log, LogLevel.Information);
            }
            catch (Exception ex)
            {
                lbs = [];
                this.ATDebug($"BLOCKS FAILED TO INITIALIZED: {ex.Message}, {ex.StackTrace}", LogMode.Log, LogLevel.Error);
            }
            return lbs;
        }

        public List<LogicBlock> SortLogicBlocks(List<LogicBlock> blocks, BlockTypes type)
        {
            List<LogicBlock> parsedBlocks = [];
            try
            {
                foreach (LogicBlock block in blocks)
                {
                    if (block.Type == type)
                        parsedBlocks.Add(block);
                }
            }
            catch (Exception ex)
            {
                this.ATDebug($"Error parsing logic blocks of type {type}: {ex.Message}", LogMode.Log, LogLevel.Error);
                return [];
            }
            return parsedBlocks;
        }

        public void ATDebug(string message, LogMode mode = LogMode.Debug, LogLevel level = LogLevel.Information)
        {
            // This is a really simple implementation, make it better in the future
            // Logging at a high frequency causes performance hits
            // Do a choice for a buffered logging to a file.
            switch (mode)
            {
                case LogMode.Log:
                    Log(message, level); break;
                case LogMode.Print:
                    Print(message); break;
                case LogMode.Debug:
                    if (DEBUG) Print(message); break;
            }
        }

        #region State Handlers
        internal void SetDefaultsHandler()
        {

        }

        internal void ConfigureHandler()
        {
            // Configuration logic can be added here
        }

        internal void DataLoadedHandler()
        {
            Register();

            List<LogicBlock> _aBlocks = ParseConfigFile(CFGPATH);
            List<LogicBlock> _sBlocks = SortLogicBlocks(_aBlocks, BlockTypes.Signal);
            List<LogicBlock> _rBlocks = SortLogicBlocks(_aBlocks, BlockTypes.Risk);
            List<LogicBlock> _uBlocks = SortLogicBlocks(_aBlocks, BlockTypes.Update);
            List<LogicBlock> _eBlocks = SortLogicBlocks(_aBlocks, BlockTypes.Execution);
            ATDebug("ALL BLOCKS SORTED", LogMode.Log, LogLevel.Information);

            _signalEngine = new(this, _sBlocks);
            _riskEngine = new(this, this, _rBlocks);
            _updateEngine = new(this, _uBlocks);
            _executionEngine = new(this);
            ATDebug("ALL ENGINES INITIALIZED", LogMode.Log, LogLevel.Information);

            ATDebug("AURORA STRATEGY INIT COMPLETE", LogMode.Log, LogLevel.Information);
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
        #endregion

        #region NinjaScript Methods
        protected override void OnBarUpdate()
        {
            try
            {
                SignalEngine.SignalProduct SGL1 = _signalEngine.Evaluate();
                RiskEngine.RiskProduct RSK1 = _riskEngine.Evaluate();

                _updateEngine.Update(UpdateEngine.UpdateTypes.OnBarUpdate);
                ExecutionEngine.ExecutionProduct EXC1 = _executionEngine.Execute(SGL1, RSK1);
            }
            catch (Exception ex)
            {
                Print($"Error in OnBarUpdate: {ex.Message}, {ex.StackTrace}");
            }
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            _updateEngine.Update(UpdateEngine.UpdateTypes.OnBarUpdate);
        }

        protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice, OrderState orderState, DateTime time, Cbi.ErrorCode error, string comment)
        {
            _updateEngine.Update(UpdateEngine.UpdateTypes.OnOrderUpdate);
        }

        protected override void OnPositionUpdate(Position position, double averagePrice, int quantity, MarketPosition marketPosition)
        {
            _updateEngine.Update(UpdateEngine.UpdateTypes.OnPositionUpdate);
        }
        #endregion
    }
}
