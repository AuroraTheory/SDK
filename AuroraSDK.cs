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

namespace AddOns.Aurora.SDK
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
        private Dictionary<int, Series<double>> _persistantStorage;

        public SignalEngine(StrategyBase Host, List<LogicBlock> LogicBlocks, Dictionary<int, Series<double>> PersistantStorage)
        {
            _host = Host;

            foreach (LogicBlock lb in LogicBlocks)
                if (lb.Type != BlockTypes.Signal) throw new ArrayTypeMismatchException();

            _logicblocks = [.. LogicBlocks];
            _persistantStorage = PersistantStorage;
        }

        public SignalProduct Evaluate()
        {
            SignalProduct SP = new();
            Dictionary<int, LogicTicket> logicOutputs = new Dictionary<int, LogicTicket>();
            int biasCount = 0;

            foreach (LogicBlock lb in _logicblocks)
            {
                logicOutputs[lb.Id] = lb.Forward();
                switch(logicOutputs[lb.Id].SubType)
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
            public string Name;
        }

        private StrategyBase _host;
        private List<LogicBlock> _logicblocks;

        public RiskEngine(StrategyBase Host, List<LogicBlock> LogicBlocks)
        {
            _host = Host;
            // clean the list of logic blocks to make sure they are all the valid type of logic block

            foreach (LogicBlock lb in LogicBlocks)
                if (lb.Type != BlockTypes.Risk)throw new ArrayTypeMismatchException();

            _logicblocks = [.. LogicBlocks];
        }

        public RiskProduct Evaluate()
        {
            // some type of persistant object to store logic outputs

            foreach (LogicBlock lb in _logicblocks)
            {
                //lb.Forward(); // save to persistant storage object
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

    }
    #endregion

    public class ExecutionEngine
    {

    }
}
