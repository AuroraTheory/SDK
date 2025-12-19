using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK.Engines
{
    public sealed class RiskEngine
    {
        public struct RiskProduct
        {
            public int Size;
        }

        private readonly Strategy _host;
        private readonly List<LogicBlock> _logicBlocks;

        public RiskEngine(Strategy host, List<LogicBlock> logicBlocks)
        {
            //_host = Guard.NotNull(host, nameof(host));
            //_logicBlocks = [.. Guard.NotNullList(_host.SortLogicBlocks(logicBlocks, BlockTypes.Risk), nameof(logicBlocks))];

            for (int i = 0; i < _logicBlocks.Count; i++)
            {
                var lb = _logicBlocks[i] ?? throw new ArgumentException("LogicBlocks contains a null element.", nameof(logicBlocks));
                Guard.Require(lb.Type == BlockTypes.Risk, $"RiskEngine expects BlockTypes.Risk blocks only (found: {lb.Type}).");
            }
        }

        public double MultiplyAll(double baseValue, IReadOnlyList<double> factors)
        {
            double result = baseValue;

            foreach (double factor in factors)
                result *= factor;

            return result;
        }


        public RiskProduct Evaluate()
        {
            var rp = new RiskProduct
            {
                Size = 0,
            };

            try
            {
                int baseContracts = 10;
                if (baseContracts < 0)
                    baseContracts = 0;

                if (_logicBlocks == null || _logicBlocks.Count == 0)
                {
                    rp.Size = baseContracts;
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
                        ticket = lb.SafeGuardForward([]);
                    }
                    catch (Exception ex)
                    {
                        //_host.ATDebug($"RiskEngine: Forward() failed for block subtype {lb.SubType}. {ex}", LogMode.Log, LogLevel.Error);
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

                        default:
                            throw new NotSupportedException($"RiskEngine: unsupported subtype: {lb.SubType}");
                    }
                }

                double scaled = MultiplyAll(baseContracts, multipliers);

                int finalContracts = (int)Math.Floor(scaled);

                if (finalContracts > contractLimit)
                    finalContracts = contractLimit;

                rp.Size = finalContracts;
                return rp;
            }
            catch (Exception ex)
            {
                //_host.ATDebug($"RiskEngine: Evaluate() exception: {ex}", LogMode.Log, LogLevel.Error);
                throw;
            }
        }
    }

}
