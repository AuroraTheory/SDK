#region Definitions
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
#endregion


namespace NinjaTrader.Custom.AddOns.Aurora.SDK
{
    public abstract partial class AuroraStrategy : Strategy
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
            private AuroraStrategy _strategy;
            private List<LogicBlock> _logicblocks;

            public SignalEngine(StrategyBase Host, AuroraStrategy Strategy, List<LogicBlock> LogicBlocks)
            {
                _host = Host;
                _strategy = Strategy;

                foreach (LogicBlock lb in LogicBlocks)
                    if (lb.Type != BlockTypes.Signal) throw new ArrayTypeMismatchException();

                _logicblocks = [.. LogicBlocks];
            }

            public SignalProduct Evaluate()
            {
                SignalProduct SP = new();
                Dictionary<int, LogicTicket> logicOutputs = [];
                int biasCount = 0;
                _strategy.ATDebug("Signal Engine: Product Initialization Complete");
                try
                {
                    _strategy.ATDebug("Signal Engine: Logic Loop Start");
                    if (_logicblocks is not null && _logicblocks.Count != 0)
                        // should we do a forloop to eval and then a forloop to aggregate?
                        foreach (LogicBlock lb in _logicblocks)
                        {
                            logicOutputs[lb.Id] = lb.Forward();
                            switch (lb.SubType)
                            {
                                case BlockSubTypes.Bias:
                                    if (logicOutputs[lb.Id].Value is not null && (bool)logicOutputs[lb.Id].Value is true)
                                        biasCount++;
                                    else
                                        biasCount--;
                                    break;
                                case BlockSubTypes.Filter:
                                    if (logicOutputs[lb.Id].Value is not null && (bool)logicOutputs[lb.Id].Value is false)
                                        return new SignalProduct
                                        {
                                            orderType = OrderType.Market,
                                            direction = MarketPosition.Flat,
                                            Name = "Filtered"
                                        };
                                    break;
                                default:
                                    _strategy.ATDebug($"tf1");
                                    break;
                            }
                        }
                    _strategy.ATDebug("Signal Engine: Logic Loop End");

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
                    _strategy.ATDebug($"Signal Engine: direction:{SP.direction}");
                }
                catch (Exception ex)
                {
                    _strategy.ATDebug($"Signal Engine: Exception: {ex.Message}");
                    return new SignalProduct
                    {
                        orderType = OrderType.Market,
                        direction = MarketPosition.Flat,
                        Name = "Error"
                    };
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
            private AuroraStrategy _strategy;
            private List<LogicBlock> _logicblocks;
            int BaseContracts { get; set; } = 1;

            public RiskEngine(StrategyBase Host, AuroraStrategy Strategy, List<LogicBlock> LogicBlocks)
            {
                _host = Host;
                _strategy = Strategy;
                BaseContracts = 10; // TODO: THIS NEEDS TO BE FIXED, CANT BE STATIC.
                                    // FIX IT DURING THE CONFIG FILE BRANCH

                // TODO: clean the list of logic blocks to make sure they are all the valid type of logic block

                foreach (LogicBlock lb in LogicBlocks)
                    if (lb.Type != BlockTypes.Risk) throw new ArrayTypeMismatchException();

                _logicblocks = [.. LogicBlocks];
            }

            public RiskProduct Evaluate()
            {
                _strategy.ATDebug("Risk Engine: Step Start");
                var rp = new RiskProduct
                {
                    size = 0,
                    name = string.Empty,
                    miscValues = new Dictionary<string, object>()
                };
                var logicOutputs = new Dictionary<int, LogicTicket>();
                double multiplier = 1.0;
                int contractLimit = int.MaxValue;
                _strategy.ATDebug("Risk Engine: Product Initialization Complete");
                try
                {
                    _strategy.ATDebug("Risk Engine: Logic Loop Start");
                    foreach (var lb in _logicblocks)
                    {
                        var output = lb.Forward();
                        logicOutputs[lb.Id] = output;

                        switch (lb.SubType)
                        {
                            case BlockSubTypes.Multiplier:
                                // best-effort cast; will throw if the underlying value isn't convertible
                                multiplier *= (double)output.Value;
                                break;

                            case BlockSubTypes.Limit:
                                contractLimit = Math.Min(contractLimit, (int)output.Value);
                                break;

                            default:
                                throw new NotSupportedException($"Unsupported block subtype: {lb.SubType}");
                        }
                    }
                    _strategy.ATDebug("Risk Engine: Step End");

                    _strategy.ATDebug($"Risk Engine: BaseContracts={BaseContracts}, Multiplier={multiplier}, ContractLimit={contractLimit}");

                    // Multiply base contracts by multiplier before rounding
                    int contracts = (int)Math.Round(BaseContracts * multiplier);

                    if (contracts > contractLimit)
                        contracts = contractLimit;

                    if (contracts < 0)
                        contracts = 0;

                    rp.size = contracts;

                    _strategy.ATDebug($"Risk Engine: Final Contract Size={rp.size}");
                }
                catch (Exception ex)
                {
                    _strategy.ATDebug($"Error in Risk Engine Evaluate: {ex.Message}");
                    return new RiskProduct
                    {
                        size = 0,
                        name = "Error",
                        miscValues = new Dictionary<string, object>()
                    };
                }
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
                try
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
                catch (Exception ex)
                {
                    _Host.Print($"Error in Execution Engine Execute: {ex.Message}");
                    return new ExecutionProduct { info = "Error" };
                }
            }
        }
        #endregion
    }
}
