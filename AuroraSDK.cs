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

namespace NinjaTrader.Custom.AddOns.Aurora.SDK
{
    public abstract partial class AuroraStrategy : Strategy
    {
        // TODO: Create a time series data structure to hold external data sources
        // TODO: Then create a dict or list to hold those datastructures to be called by logic blocks
        [NinjaScriptProperty, Display(Name = "DEBUG MODE", GroupName = "Aurora Settings")]
        public bool DEBUG { get; set; } = false;

        [NinjaScriptProperty, Display(Name = "CONFIG FILE", GroupName = "Aurora Settings")]
        public string CFGPATH { get; set; } = "";


        private SignalEngine _signalEngine;
        private RiskEngine _riskEngine;
        private UpdateEngine _updateEngine;
        private ExecutionEngine _executionEngine;

        private List<LogicBlock> Blocks;

        public enum LogMode
        {
            Log,
            Print,
            Debug
        }

        public List<LogicBlock> ParseConfigFile(string filePath)
        {
            AlgoConfig algoConfig = new();
            LogicBlockFactory blockFactory = new();
            List<LogicBlock> lbs = new();
            try
            {
                algoConfig = YamlRawLoader.Load(filePath);
                foreach (LogicBlockConfig lbc in algoConfig.Logic)
                {
                    LogicBlock lb = blockFactory.Create(lbc.BID, this, lbc.BParameters);
                    lbs.Add(lb);
                }
            }
            catch (Exception ex)
            {
                lbs = [];
                this.ATDebug($"Exception in ParseConfigFile {ex.Message}");
            }
            return lbs;
        }

        public void ATDebug(string message, LogMode mode = LogMode.Log, LogLevel level = LogLevel.Information)
        {
            // This is a really simple implementation, make it better in the future
            // Logging at a high frequency causes performance hits
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

        internal void SetDefaultsHandler()
        {

        }

        internal void ConfigureHandler()
        {
            // Configuration logic can be added here
        }

        internal void DataLoadedHandler()
        {
            List<LogicBlock> _aBlocks = ParseConfigFile(CFGPATH);
            List<LogicBlock> _sBlocks = ParseLogicBlocks(Blocks, BlockTypes.Signal);
            List<LogicBlock> _rBlocks = ParseLogicBlocks(Blocks, BlockTypes.Risk);
            List<LogicBlock> _uBlocks = ParseLogicBlocks(Blocks, BlockTypes.Update);
            List<LogicBlock> _eBlocks = ParseLogicBlocks(Blocks, BlockTypes.Execution);

            _signalEngine = new(this, this, _sBlocks);
            _riskEngine = new(this, this, _rBlocks);
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
                Print($"Error parsing logic blocks of type {type}: {ex.Message}");
                return [];
            }
            return parsedBlocks;
        }

        protected override void OnBarUpdate()
        {
            try
            {
                ATDebug("OnBarUpdate: Triggered", LogMode.Debug);
                /*
                Too Lazy to finish the fancy debug log shit rn

                if (_signalEngine == null || _riskEngine == null || _updateEngine == null || _executionEngine == null)
                {
                    ATDebug("OnBarUpdate: Failed to ")
                    return;
                }
                */
                SignalEngine.SignalProduct SGL1 = _signalEngine.Evaluate();
                RiskEngine.RiskProduct RSK1 = _riskEngine.Evaluate();

                _updateEngine.Update(UpdateEngine.UpdateTypes.OnBarUpdate);
                ExecutionEngine.ExecutionProduct EXC1 = _executionEngine.Execute(SGL1, RSK1);
            }
            catch (Exception ex)
            {
                Print($"Error in OnBarUpdate: {ex.Message}");
            }
        }
    }
}
