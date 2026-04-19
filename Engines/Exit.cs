using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK.Engines
{
    public sealed class ExitHandler
    {
        public struct ExitProduct
        {
            public int Size;
        }

#pragma warning disable CS0436 // Type conflicts with imported type
        private readonly Strategy _host;
#pragma warning restore CS0436 // Type conflicts with imported type
        private readonly List<Block.LogicBlock> _logicBlocks;

#pragma warning disable CS0436 // Type conflicts with imported type
        public ExitHandler(Strategy host, List<Block.LogicBlock> logicBlocks)
#pragma warning restore CS0436 // Type conflicts with imported type
        {
            //_host = Guard.NotNull(host, nameof(host));
            //_logicBlocks = [.. Guard.NotNullList(_host.SortLogicBlocks(logicBlocks, BlockTypes.Risk), nameof(logicBlocks))];

            for (int i = 0; i < _logicBlocks.Count; i++)
            {
                var lb = _logicBlocks[i] ?? throw new ArgumentException("LogicBlocks contains a null element.", nameof(logicBlocks));
            }
        }

        public double MultiplyAll(double baseValue, IReadOnlyList<double> factors)
        {
            double result = baseValue;

            foreach (double factor in factors)
                result *= factor;

            return result;
        }


        public ExitProduct Evaluate()
        {
            var rp = new ExitProduct
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
			
                    try
                    {
                        var ticket = lb.SafeGuardForward([]);
						
						if (ticket == null)
                        	continue;
						
						switch (lb.SubType)
                    	{
                        	case Block.BlockSubTypes.Multiplier:
                            	if (SafeGuard.Safe.TryToDouble(ticket, out var mul))
                                	multipliers.Add(mul);
                            	break;

                        	case Block.BlockSubTypes.Limit:
                            	if (SafeGuard.Safe.TryToInt(ticket, out var lim))
                                	contractLimit = Math.Min(contractLimit, Math.Max(0, lim));
                            	break;

                        	default:
                            	throw new NotSupportedException($"RiskEngine: unsupported subtype: {lb.SubType}");
                    	}
					}
                    catch (Exception ex)
                    {
                        //_host.ATDebug($"RiskEngine: Forward() failed for block subtype {lb.SubType}. {ex}", LogMode.Log, LogLevel.Error);
                        throw;
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
