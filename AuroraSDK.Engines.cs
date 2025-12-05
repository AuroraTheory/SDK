#region Definitions
using System;
using System.Text;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.NinjaScript.Indicators;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Diagnostics;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Controls;
using NinjaTrader.CQG.ProtoBuf;
using System.Linq;
using NinjaTrader.NinjaScript;
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

        public RiskEngine(StrategyBase Host, List<LogicBlock> LogicBlocks)
        {
            _host = Host;
            // TODO: clean the list of logic blocks to make sure they are all the valid type of logic block

            foreach (LogicBlock lb in LogicBlocks)
                if (lb.Type != BlockTypes.Risk) throw new ArrayTypeMismatchException();

            _logicblocks = [.. LogicBlocks];
        }

        public RiskProduct Evaluate(SignalEngine.SignalProduct SP1)
        {
            List<LogicTicket> logicOutputs = new List<LogicTicket>();

            foreach (LogicBlock lb in _logicblocks)
            {
                logicOutputs.Add(lb.Forward());
            }

            // do some further processing to get the risk product

            return new RiskProduct
            {
                
            };
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
            throw new NotImplementedException();
        }

        public void Update(UpdateTypes type)
        {
            throw new NotImplementedException();
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

        public ExecutionProduct Execute(SignalEngine.SignalProduct SP1, RiskEngine.RiskProduct RP1)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}