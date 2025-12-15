#region Definitions
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
#endregion

namespace NinjaTrader.Custom.Strategies.Aurora.SDK
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

            private AuroraStrategy _host;
            private List<LogicBlock> _logicblocks;

            public SignalEngine(AuroraStrategy Strategy, List<LogicBlock> LogicBlocks)
            {
                _host = Strategy;

                foreach (LogicBlock lb in LogicBlocks)
                {
                    if (lb.Type != BlockTypes.Signal || (lb.SubType != BlockSubTypes.Bias && lb.SubType != BlockSubTypes.Filter)) throw new ArrayTypeMismatchException();
                }


                _logicblocks = [.. LogicBlocks];
            }

            public SignalProduct Evaluate()
            {
                SignalProduct SP = new();
                Dictionary<int, LogicTicket> logicOutputs = [];
                int biasCount = 0;
                try
                {
                    if (_logicblocks is not null && _logicblocks.Count != 0)
                    {
                        foreach (LogicBlock lb in _logicblocks)
                        {
                            LogicTicket lt1 = lb.Forward();
                            switch (lb.SubType)
                            {
                                case BlockSubTypes.Bias:
                                    if (lt1.Values[0] is not null)
                                        if ((int)lt1.Values[0] == 1)
                                            biasCount++;
                                        else if ((int)lt1.Values[0] == -1)
                                            biasCount--;
                                    break;
                                case BlockSubTypes.Filter:
                                    if (lt1.Values[0] is not null)
                                        if ((bool)lt1.Values[0] is false)
                                            SP = new SignalProduct
                                            {
                                                orderType = OrderType.Market,
                                                direction = MarketPosition.Flat,
                                                Name = "Filtered"
                                            };
                                    break;
                                default:
                                    _host.ATDebug($"tf1");
                                    break;
                            }
                        }
                    }
                    else
                        _host.ATDebug("Null Logic Blocks", LogMode.Debug);

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
                }
                catch (Exception ex)
                {
                    _host.ATDebug($"Signal Engine: Exception: {ex.Message}, {ex.StackTrace}", LogMode.Log, LogLevel.Error);
                    throw;
                }

                //_host.ATDebug($"Signal Engine Completed: direction:{SP.direction}, name: {SP.Name}");
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
                var rp = new RiskProduct
                {
                    size = 0,
                    name = string.Empty,
                    miscValues = []
                };
                var logicOutputs = new Dictionary<int, LogicTicket>();
                double multiplier = 1.0;
                int contractLimit = int.MaxValue;
                try
                {
                    if (_logicblocks is not null && _logicblocks.Count != 0)
                    {
                        foreach (var lb in _logicblocks)
                        {
                            LogicTicket output = lb.Forward();
                            logicOutputs[lb.Id] = output;

                            if (output.Values.Count != 0)
                            switch (lb.SubType)
                            {
                                case BlockSubTypes.Multiplier:
                                    multiplier *= (double)output.Values[0];
                                    break;

                                case BlockSubTypes.Limit:
                                    contractLimit = Math.Min(contractLimit, (int)output.Values[0]);
                                    break;

                                case BlockSubTypes.Extra:
                                    rp.miscValues[output.Values[1].ToString()] = (double)output.Values[0];
                                    break;
                                default:
                                    throw new NotSupportedException($"Unsupported block subtype: {lb.SubType}");
                            }
                        }
                    }

                    // Multiply base contracts by multiplier before rounding
                    int contracts = (int)Math.Round(BaseContracts * multiplier);

                    if (contracts > contractLimit)
                        contracts = contractLimit;

                    if (contracts < 0)
                        contracts = 0;

                    rp.size = contracts;

                    //_strategy.ATDebug($"Risk Engine Completed: Contracts={contracts}, Multiplier={multiplier}");
                }
                catch (Exception ex)
                {
                    _strategy.ATDebug($"Error in Risk Engine Evaluate: {ex.Message}, {ex.StackTrace}", LogMode.Log, LogLevel.Error);
                    throw;
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

            private AuroraStrategy _host;
            private List<LogicBlock> _blocks;

            public UpdateEngine(AuroraStrategy Host, List<LogicBlock> LogicBlocks)
            {
                _host = Host;
            }

            public void Update(UpdateTypes type)
            {
            }
        }
        #endregion

        #region Execution Engine
        public class ExecutionEngine(AuroraStrategy Host)
        {
            public struct ExecutionProduct
            {
                public string info;
            }

            AuroraStrategy _Host = Host;

            public ExecutionProduct Execute(SignalEngine.SignalProduct SP1, RiskEngine.RiskProduct RP1)
            {
                ExecutionProduct exp;
                try
                {
                    if (SP1.direction == MarketPosition.Flat || RP1.size == 0)
                    {
                        exp = new ExecutionProduct { info = "No Signal" };
                    }
                    else if (SP1.direction == MarketPosition.Long)
                    {
                        _Host.EnterLong(RP1.size, "Long_Aurora");
                        exp = new ExecutionProduct { info = $"Entering Long {RP1.size} contracts" };
                    }
                    else if (SP1.direction == MarketPosition.Short)
                    {
                        _Host.EnterShort(RP1.size, "Short_Aurora");
                        exp = new ExecutionProduct { info = $"Entering Short {RP1.size} contracts" };
                    }
                    else
                    {
                        exp = new ExecutionProduct { info = "Invalid Signal" };
                    }
                    if (RP1.miscValues.Count > 0 && RP1.miscValues["sarAccel"] != null)
                    {
                        _Host.SetParabolicStop(CalculationMode.Percent, (double)RP1.miscValues["sarAccel"]);
                    }
                }
                catch (Exception ex)
                {
                    _Host.ATDebug($"Exception in Execution Engine: {ex.Message}, {ex.StackTrace}", LogMode.Log, LogLevel.Error);
                    exp = new ExecutionProduct { info = "Error" };
                }
                return exp;
            }
        }
        #endregion
    }
}
