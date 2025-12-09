#region Definitions
using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using static NinjaTrader.Custom.AddOns.Aurora.SDK.SignalEngine;
using static NinjaTrader.Custom.AddOns.Aurora.SDK.RiskEngine;
using static NinjaTrader.Custom.AddOns.Aurora.SDK.UpdateEngine;
using static NinjaTrader.Custom.AddOns.Aurora.SDK.ExecutionEngine;
#endregion


namespace NinjaTrader.Custom.AddOns.Aurora.SDK
{
    #region Signal Engine
    public class SignalEngine
    {
        // the purpose of the signal engine is to loop through signal logic blocks to spit out either Long Short or Neutral.
        // the challenge comes when summarizing the outputs of the logic block

        public struct SignalProduct
        {
            public OrderType orderType;
            public MarketPosition direction;
            public string Name;
        }

        private StrategyBase _host;
        private List<LogicBlock> _logicblocks;

        public SignalEngine(StrategyBase Host, List<LogicBlock> LogicBlocks)
        {
            _host = Host;

            foreach (LogicBlock lb in LogicBlocks)
                if (lb.Type != BlockTypes.Signal) throw new ArrayTypeMismatchException();

            _logicblocks = [.. LogicBlocks];
        }

        public SignalProduct Evaluate()
        {
            SignalProduct SP = new();
            Dictionary<int, LogicTicket> logicOutputs = new Dictionary<int, LogicTicket>();
            int biasCount = 0;

            foreach (LogicBlock lb in _logicblocks)
            {
                logicOutputs[lb.Id] = lb.Forward();
                switch (logicOutputs[lb.Id].SubType)
                {
                    case BlockSubTypes.Bias:
                        if (logicOutputs[lb.Id].Value is true)
                            biasCount++;
                        else
                            biasCount--;
                        break;
                    case BlockSubTypes.Filter:
                        if (logicOutputs[lb.Id].Value is false)
                            return new SignalProduct
                            {
                                orderType = OrderType.Market,
                                direction = MarketPosition.Flat,
                                Name = "Filtered"
                            };
                        break;
                    default:
                        throw new NotImplementedException(); // TODO: implement correct exception
                }
            }

            if (biasCount > 0)
            {
                SP.direction = MarketPosition.Long;
                SP.orderType = OrderType.Market;
                SP.Name = "Long Bias";
            }
            else if (biasCount < 0)
            {
                SP.direction = MarketPosition.Short;
                SP.orderType = OrderType.Market;
                SP.Name = "Short Bias";
            }
            else
            {
                SP.direction = MarketPosition.Flat;
                SP.orderType = OrderType.Market;
                SP.Name = "Neutral Bias";
            }

            return SP;
        }
    }
    #endregion

    #region Risk Engine
    public class RiskEngine
    {
        public struct RiskProduct
        {
            public int size;
            public string name;
            public Dictionary<string, object> miscValues;
        }

        private StrategyBase _host;
        private List<LogicBlock> _logicblocks;
        int BaseContracts { get; set; } = 1;

        public RiskEngine(StrategyBase Host, List<LogicBlock> LogicBlocks, int BaseCons)
        {
            _host = Host;
            BaseContracts = BaseCons;
            // TODO: clean the list of logic blocks to make sure they are all the valid type of logic block

            foreach (LogicBlock lb in LogicBlocks)
                if (lb.Type != BlockTypes.Risk) throw new ArrayTypeMismatchException();

            _logicblocks = [.. LogicBlocks];
        }

        public RiskProduct Evaluate()
        {
            var rp = new RiskProduct();
            var logicOutputs = new Dictionary<int, LogicTicket>();
            double multiplier = 1.0;
            int contractLimit = int.MaxValue;

            foreach (var lb in _logicblocks)
            {
                var output = lb.Forward();
                logicOutputs[lb.Id] = output;

                switch (output.SubType)
                {
                    case BlockSubTypes.Multiplier:
                        multiplier *= (double)output.Value;
                        break;

                    case BlockSubTypes.Limit:
                        contractLimit = Math.Min(contractLimit, (int)output.Value);
                        break;

                    default:
                        throw new NotSupportedException($"Unsupported block subtype: {output.SubType}");
                }
            }

            int contracts = (int)Math.Round(multiplier);

            if (contracts > contractLimit)
                contracts = contractLimit;

            if (contracts < 0)
                contracts = 0;

            rp.size = contracts;
            return rp;
        }
	}

        #endregion

        #region Update Engine
        public class UpdateEngine
        {
            // update engine will have multiple methods to be called from the strategy core methods
            public enum UpdateTypes
            {
                OnBarUpdate,
                OnExecutionUpdate,
                OnOrderUpdate,
                OnPositionUpdate
            }

            public UpdateEngine(StrategyBase Host, List<LogicBlock> LogicBlocks)
            {
                //throw new NotImplementedException();
            }

            public void Update(UpdateTypes type)
            {
                //throw new NotImplementedException();
            }
        }
        #endregion

        #region Execution Engine
        public class ExecutionEngine
        {
            public struct ExecutionProduct
            {
                public string info;
            }

            StrategyBase _Host;
            List<LogicBlock> _LogicBlocks;

            public ExecutionEngine(StrategyBase Host, List<LogicBlock> LogicBlocks)
            {
                _Host = Host;
                _LogicBlocks = LogicBlocks;
                foreach (LogicBlock lb in LogicBlocks)
                    if (lb.Type != BlockTypes.Execution) throw new ArrayTypeMismatchException();
            }

            public ExecutionProduct Execute(SignalEngine.SignalProduct SP1, RiskEngine.RiskProduct RP1)
            {
                if (SP1.direction == MarketPosition.Flat || RP1.size == 0)
                {
                    return new ExecutionProduct { info = "No Signal" };
                }
                else if (SP1.direction == MarketPosition.Long)
                {
                    _Host.EnterLong(RP1.size, "Long_Aurora");
                    return new ExecutionProduct { info = $"Entering Long {RP1.size} contracts" };
                }
                else if (SP1.direction == MarketPosition.Short)
                {
                    _Host.EnterShort(RP1.size, "Short_Aurora");
                    return new ExecutionProduct { info = $"Entering Short {RP1.size} contracts" };
                }
                else
                {
                   return new ExecutionProduct { info = "Invalid Signal" };
                }
            }
        }
        #endregion
    }
