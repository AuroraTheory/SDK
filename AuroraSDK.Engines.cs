#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace NinjaTrader.Custom.Strategies.Aurora.SDK
{
    public abstract partial class AuroraStrategy : Strategy
    {
        #region Engine helpers

        public static class Guard
        {
            public static T NotNull<T>(T value, string paramName) where T : class
            {
                if (value == null)
                    throw new ArgumentNullException(paramName);
                return value;
            }

            public static IList<T> NotNullList<T>(IList<T> value, string paramName)
            {
                if (value == null)
                    throw new ArgumentNullException(paramName);
                return value;
            }

            public static void Require(bool condition, string message)
            {
                if (!condition)
                    throw new ArgumentException(message);
            }
        }

        public static class Safe
        {
            public static bool TryGetAt(IList<object> values, int index, out object value)
            {
                value = null;
                if (values == null || index < 0 || index >= values.Count)
                    return false;

                value = values[index];
                return value != null;
            }

            public static bool TryToMarketPosition(object value, out MarketPosition mp)
            {
                mp = MarketPosition.Flat;
                if (value == null)
                    return false;

                if (value is MarketPosition m)
                {
                    mp = m;
                    return true;
                }

                // Allow int-backed enums or strings if upstream blocks emit them.
                try
                {
                    if (value is int i)
                    {
                        mp = (MarketPosition)i;
                        return true;
                    }

                    if (value is string s && Enum.TryParse(s, true, out MarketPosition parsed))
                    {
                        mp = parsed;
                        return true;
                    }
                }
                catch
                {
                    // Intentionally swallow; caller decides behavior.
                }

                return false;
            }

            public static bool TryToBool(object value, out bool b)
            {
                b = false;
                if (value == null)
                    return false;

                if (value is bool bb)
                {
                    b = bb;
                    return true;
                }

                try
                {
                    b = Convert.ToBoolean(value);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public static bool TryToDouble(object value, out double d)
            {
                d = 0;
                if (value == null)
                    return false;

                if (value is double dd)
                {
                    d = dd;
                    return true;
                }

                try
                {
                    d = Convert.ToDouble(value);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public static bool TryToInt(object value, out int i)
            {
                i = 0;
                if (value == null)
                    return false;

                if (value is int ii)
                {
                    i = ii;
                    return true;
                }

                try
                {
                    i = Convert.ToInt32(value);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public static bool TryGetDouble(IDictionary<string, object> dict, string key, out double value)
            {
                value = 0;
                if (dict == null || string.IsNullOrWhiteSpace(key))
                    return false;

                if (!dict.TryGetValue(key, out var raw))
                    return false;

                return TryToDouble(raw, out value);
            }
        }

        #endregion

        #region Signal Engine

        public sealed class SignalEngine
        {
            public struct SignalProduct
            {
                public OrderType OrderType;
                public MarketPosition Direction;
                public string Name;

                public static SignalProduct Flat(string name)
                {
                    return new SignalProduct
                    {
                        OrderType = OrderType.Market,
                        Direction = MarketPosition.Flat,
                        Name = name ?? string.Empty
                    };
                }
            }

            private readonly AuroraStrategy _host;
            private readonly List<LogicBlock> _logicBlocks;

            public SignalEngine(AuroraStrategy host, List<LogicBlock> logicBlocks)
            {
                _host = Guard.NotNull(host, nameof(host));
                _logicBlocks = [.. Guard.NotNullList(logicBlocks, nameof(logicBlocks))];

                bool hasBias = false;
                bool hasSignal = false;

                for (int i = 0; i < _logicBlocks.Count; i++)
                {
                    var lb = _logicBlocks[i];
                    if (lb == null)
                        throw new ArgumentException("LogicBlocks contains a null element.", nameof(logicBlocks));

                    Guard.Require(lb.Type == BlockTypes.Signal, $"SignalEngine expects BlockTypes.Signal blocks only (found: {lb.Type}).");

                    bool validSubtype =
                        lb.SubType == BlockSubTypes.Bias ||
                        lb.SubType == BlockSubTypes.Filter ||
                        lb.SubType == BlockSubTypes.Signal;

                    Guard.Require(validSubtype, $"SignalEngine received unsupported subtype: {lb.SubType}.");

                    if (lb.SubType == BlockSubTypes.Bias)
                    {
                        if (hasBias)
                            _host.ATDebug("SignalEngine: multiple bias blocks detected.", LogMode.Debug);
                        hasBias = true;
                    }

                    if (lb.SubType == BlockSubTypes.Signal)
                    {
                        if (hasSignal)
                            _host.ATDebug("SignalEngine: multiple signal blocks detected.", LogMode.Debug);
                        hasSignal = true;
                    }
                }

                if (!hasSignal)
                    _host.ATDebug("SignalEngine: no Signal subtype block found; engine will likely remain Neutral.", LogMode.Debug);

                if (!hasBias)
                    _host.ATDebug("SignalEngine: no Bias subtype block found; engine will likely remain Neutral.", LogMode.Debug);
            }

            public SignalProduct Evaluate()
            {
                try
                {
                    if (_logicBlocks == null || _logicBlocks.Count == 0)
                    {
                        _host.ATDebug("SignalEngine: no logic blocks configured.", LogMode.Debug);
                        return SignalProduct.Flat("No Blocks");
                    }

                    MarketPosition bias = MarketPosition.Flat;
                    MarketPosition signal = MarketPosition.Flat;

                    for (int i = 0; i < _logicBlocks.Count; i++)
                    {
                        var lb = _logicBlocks[i];
                        if (lb == null || !lb.Initialized)
                            continue;

                        LogicTicket ticket;
                        try
                        {
                            ticket = lb.Forward();
                        }
                        catch (Exception ex)
                        {
                            _host.ATDebug($"SignalEngine: Forward() failed for block subtype {lb.SubType}. {ex}", LogMode.Log, LogLevel.Error);
                            throw;
                        }

                        var values = ticket.Values;
                        if (values == null || values.Count == 0)
                            continue;

                        switch (lb.SubType)
                        {
                            case BlockSubTypes.Signal:
                                // Purpose:
                                // - Read the primary output of a Signal block (expected at values[0]).
                                // - Convert that output to a MarketPosition and set the 'signal' marker.
                                // Behavior details:
                                // - If values[0] contains a MarketPosition, it is used directly.
                                // - If values[0] is an int (enum-backed) or a string ("Long"/"Short"/"Flat"), Safe.TryToMarketPosition
                                //   will attempt conversion. If conversion fails, this block is ignored.
                                // - This value represents the runtime signal direction produced by the block.
                                if (Safe.TryGetAt(values, 0, out var s0) && Safe.TryToMarketPosition(s0, out var sMp))
                                    signal = sMp;
                                break;

                            case BlockSubTypes.Bias:
                                // Purpose:
                                // - Read a persistent or higher-level directional bias (expected at values[0]).
                                // - Convert that output to a MarketPosition and set the 'bias' marker.
                                // Behavior details:
                                // - Bias often represents longer-term or committee-driven direction.
                                // - Same flexible conversions as Signal (MarketPosition, int, string) are supported.
                                // - Multiple Bias blocks may be present; last valid assignment in iteration wins.
                                if (Safe.TryGetAt(values, 0, out var b0) && Safe.TryToMarketPosition(b0, out var bMp))
                                    bias = bMp;
                                break;

                            case BlockSubTypes.Filter:
                                // Purpose:
                                // - Determine whether the current market/context should be filtered out (no trades).
                                // - The Filter block is expected to emit a boolean-like value at values[0].
                                // Behavior details:
                                // - Safe.TryToBool handles explicit bools and convertible types (e.g., 0/1, "true"/"false").
                                // - If the Filter returns true (isFiltered), the engine immediately returns a Flat SignalProduct
                                //   with the name "Filtered" to indicate that trading should be suppressed for this evaluation cycle.
                                // - This is an early exit so filters take precedence over bias/signal reconciliation.
                                if (Safe.TryGetAt(values, 0, out var f0) && Safe.TryToBool(f0, out var isFiltered) && isFiltered)
                                    return SignalProduct.Flat("Filtered");
                                break;

                            default:
                                // Unexpected subtype; log a warning and continue.
                                _host.ATDebug($"SignalEngine: unexpected subtype reached: {lb.SubType}", LogMode.Log, LogLevel.Warning);
                                break;
                        }
                    }

                    // Reconcile final action:
                    // - Only enter a direction when both bias and signal agree (both Long or both Short).
                    if (bias == MarketPosition.Long && signal == MarketPosition.Long)
                        return new SignalProduct { Direction = MarketPosition.Long, OrderType = OrderType.Market, Name = "Long Bias" };

                    if (bias == MarketPosition.Short && signal == MarketPosition.Short)
                        return new SignalProduct { Direction = MarketPosition.Short, OrderType = OrderType.Market, Name = "Short Bias" };

                    // Default neutral outcome when bias and signal don't both agree.
                    return SignalProduct.Flat("Neutral Bias");
                }
                catch (Exception ex)
                {
                    _host.ATDebug($"SignalEngine: Evaluate() exception: {ex}", LogMode.Log, LogLevel.Error);
                    throw;
                }
            }
        }

        #endregion

        #region Risk Engine

        public sealed class RiskEngine
        {
            public struct RiskProduct
            {
                public int Size;
                public string Name;
                public Dictionary<string, object> MiscValues;
            }

            private readonly AuroraStrategy _host;
            private readonly List<LogicBlock> _logicBlocks;

            public RiskEngine(AuroraStrategy host, List<LogicBlock> logicBlocks)
            {
                _host = Guard.NotNull(host, nameof(host));
                _logicBlocks = [.. Guard.NotNullList(logicBlocks, nameof(logicBlocks))];

                for (int i = 0; i < _logicBlocks.Count; i++)
                {
                    var lb = _logicBlocks[i] ?? throw new ArgumentException("LogicBlocks contains a null element.", nameof(logicBlocks));
                    Guard.Require(lb.Type == BlockTypes.Risk, $"RiskEngine expects BlockTypes.Risk blocks only (found: {lb.Type}).");
                }
            }

            public RiskProduct Evaluate()
            {
                var rp = new RiskProduct
                {
                    Size = 0,
                    Name = string.Empty,
                    MiscValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                };

                try
                {
                    int baseContracts = _host.BASECONTRACTS;
                    if (baseContracts < 0)
                        baseContracts = 0;

                    if (_logicBlocks == null || _logicBlocks.Count == 0)
                    {
                        rp.Size = baseContracts;
                        rp.Name = "BaseOnly";
                        return rp;
                    }

                    var multipliers = new List<double>();
                    int contractLimit = int.MaxValue;

                    for (int i = 0; i < _logicBlocks.Count; i++)
                    {
                        var lb = _logicBlocks[i];
                        if (lb == null || !lb.Initialized)
                            continue;

                        LogicTicket ticket;
                        try
                        {
                            ticket = lb.Forward();
                        }
                        catch (Exception ex)
                        {
                            _host.ATDebug($"RiskEngine: Forward() failed for block subtype {lb.SubType}. {ex}", LogMode.Log, LogLevel.Error);
                            throw;
                        }

                        var values = ticket.Values;
                        if (values == null || values.Count == 0)
                            continue;

                        switch (lb.SubType)
                        {
                            case BlockSubTypes.Multiplier:
                                if (Safe.TryGetAt(values, 0, out var m0) && Safe.TryToDouble(m0, out var mul))
                                    multipliers.Add(mul);
                                break;

                            case BlockSubTypes.Limit:
                                if (Safe.TryGetAt(values, 0, out var l0) && Safe.TryToInt(l0, out var lim))
                                    contractLimit = Math.Min(contractLimit, Math.Max(0, lim));
                                break;

                            case BlockSubTypes.Extra:
                                {
                                    if (!Safe.TryGetAt(values, 1, out var k1))
                                        break;

                                    var key = Convert.ToString(k1) ?? string.Empty;
                                    if (string.IsNullOrWhiteSpace(key))
                                        break;

                                    if (Safe.TryGetAt(values, 0, out var v0))
                                        rp.MiscValues[key] = v0;

                                    break;
                                }

                            default:
                                throw new NotSupportedException($"RiskEngine: unsupported subtype: {lb.SubType}");
                        }
                    }

                    double scaled = _host.MultiplyAll(baseContracts, multipliers);

                    int finalContracts = (int)Math.Floor(scaled);

                    if (finalContracts > contractLimit)
                        finalContracts = contractLimit;

                    rp.Size = finalContracts;
                    rp.Name = "Computed";
                    return rp;
                }
                catch (Exception ex)
                {
                    _host.ATDebug($"RiskEngine: Evaluate() exception: {ex}", LogMode.Log, LogLevel.Error);
                    throw;
                }
            }
        }

        #endregion

        #region Update Engine

        public sealed class UpdateEngine
        {
            public enum UpdateTypes
            {
                OnBarUpdate,
                OnExecutionUpdate,
                OnOrderUpdate,
                OnPositionUpdate
            }

            private readonly AuroraStrategy _host;
            private readonly List<LogicBlock> _blocks;

            public UpdateEngine(AuroraStrategy host, List<LogicBlock> logicBlocks)
            {
                _host = Guard.NotNull(host, nameof(host));
                _blocks = logicBlocks != null ? new List<LogicBlock>(logicBlocks) : new List<LogicBlock>();
            }

            public void Update(UpdateTypes type)
            {
                // Negative-space: do nothing unless there is work to do.
                if (_blocks == null || _blocks.Count == 0)
                    return;

                // Hook for future:
                // - route updates to blocks that care about this update type
                // - isolate per-block failures so a single block cannot silently corrupt state
                for (int i = 0; i < _blocks.Count; i++)
                {
                    var lb = _blocks[i];
                    if (lb == null || !lb.Initialized)
                        continue;

                    // TODO: implement per-block update dispatch.
                }
            }
        }

        #endregion

        #region Execution Engine

        public sealed class ExecutionEngine
        {
            public struct ExecutionProduct
            {
                public string Info;
            }

            private readonly AuroraStrategy _host;

            private const string LongSignalName = "Long_Aurora";
            private const string ShortSignalName = "Short_Aurora";
            private List<LogicBlock> _logicBlocks;

            public ExecutionEngine(AuroraStrategy host, List<LogicBlock> logicBlocks)
            {
                _host = Guard.NotNull(host, nameof(host));
                _logicBlocks = logicBlocks;
            }

            public ExecutionProduct Execute(SignalEngine.SignalProduct sp, RiskEngine.RiskProduct rp)
            {
                try
                {
                    if (sp.Direction == MarketPosition.Flat)
                        return new ExecutionProduct { Info = "No Signal" };

                    if (sp.Direction == MarketPosition.Long)
                    {
                        _host.EnterLong(rp.Size, LongSignalName + $"_{_host.keyValuePairs["TradesThisChunk"]}");

                        foreach (LogicBlock lb in _logicBlocks.Where(lb => lb.SubType == BlockSubTypes.Execution))
                            lb.Forward();

                        return new ExecutionProduct { Info = $"Entering Long {rp.Size} contracts" };
                    }
                    if (sp.Direction == MarketPosition.Short)
                    {
                        _host.EnterShort(rp.Size, ShortSignalName + $"_{_host.keyValuePairs["TradesThisChunk"]}");

                        foreach (LogicBlock lb in _logicBlocks.Where(lb => lb.SubType == BlockSubTypes.Execution))
                            lb.Forward();

                        return new ExecutionProduct { Info = $"Entering Short {rp.Size} contracts" };
                    }

                    _host.ATDebug("ExecutionEngine: invalid MarketPosition in SignalProduct.", LogMode.Log, LogLevel.Warning);
                    return new ExecutionProduct { Info = "Invalid Signal" };
                }
                catch (Exception ex)
                {
                    _host.ATDebug($"ExecutionEngine: Execute() exception: {ex}", LogMode.Log, LogLevel.Error);
                    return new ExecutionProduct { Info = "Error" };
                }
            }
        }

        #endregion
    }
}
