using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace NinjaTrader.Custom.Strategies.Aurora.SDK
{
    public abstract partial class AuroraStrategy : Strategy
    {
        internal sealed class LogicBlockBuilder
        {
            private readonly ILogicBlockFactory _factory;

            public LogicBlockBuilder(ILogicBlockFactory factory)
            {
                _factory = factory;
            }

            public List<LogicBlock> Build(AuroraStrategy host, List<LogicBlockConfig> configs)
            {
                var blocks = new List<LogicBlock>();

                foreach (LogicBlockConfig cfg in configs)
                {
                    LogicBlock block = _factory.Create(cfg.BID, host, cfg.BParameters);

                    blocks.Add(block);
                }

                return blocks;
            }
        }

        public static class LogicBlockRegistry
        {
            private static readonly Dictionary<string, Func<AuroraStrategy, Dictionary<string, object>, LogicBlock>> _map = [];

            public static void Register(string blockId, Func<AuroraStrategy, Dictionary<string, object>, LogicBlock> factory)
            {
                if (_map.ContainsKey(blockId))
                    return;

                _map[blockId] = factory;
            }

            public static Func<AuroraStrategy, Dictionary<string, object>, LogicBlock> Create(string blockId, AuroraStrategy host, Dictionary<string, object> parameters)
            {
                if (_map[blockId] == null)
                    throw new InvalidOperationException($"Unknown BID {blockId}");

                return _map[blockId];
            }
        }

        public interface ILogicBlockFactory
        {
            LogicBlock Create(
              string blockId,
              AuroraStrategy host,
              Dictionary<string, object> parameters);
        }

        public sealed class LogicBlockFactory : ILogicBlockFactory
        {
            public LogicBlock Create(string blockId, AuroraStrategy host, Dictionary<string, object> parameters)
            {
                Func<AuroraStrategy, Dictionary<string, object>, LogicBlock> fact = LogicBlockRegistry.Create(blockId, host, parameters);
                return fact(host, parameters);
            }
        }

        public sealed class AlgoConfig
        {
            public List<LogicBlockConfig> Logic { get; set; }

            public override string ToString()
            {
                if (Logic == null || Logic.Count == 0)
                    return "AlgoConfig { Logic=[] }";

                return $"AlgoConfig {{ LogicCount={Logic.Count}, Logic=[{string.Join("; ", Logic)}] }}";
            }
        }

        public sealed class LogicBlockConfig
        {
            public string BID { get; set; }
            public string DPID { get; set; }

            public string BType { get; set; }
            public string BSubType { get; set; }

            public Dictionary<string, object> BParameters { get; set; }
            public DebugConfig Debug { get; set; }

            public override string ToString()
            {
                string parametersStr =
                  BParameters == null || BParameters.Count == 0
                    ? "{}"
                    : "{" + string.Join(
                        ", ",
                        BParameters
                          .OrderBy(kv => kv.Key)
                          .Select(kv => $"{kv.Key}={kv.Value}")
                      ) + "}";

                return
                  $"LogicBlockConfig(" +
                  $"BID={BID}, " +
                  $"DPID={DPID}, " +
                  $"BType={BType}, " +
                  $"BSubType={BSubType}, " +
                  $"BParameters={parametersStr}, " +
                  $"Debug={Debug}" +
                  $")";
            }
        }

        public sealed class DebugConfig
        {
            public bool Enabled { get; set; }
            public string Type { get; set; }
        }

        public static class YamlRawLoader
        {
            public static AlgoConfig Load(string filePath)
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException(filePath);

                var deserializer = new DeserializerBuilder()
                  .IgnoreUnmatchedProperties()
                  .Build();

                using var reader = new StreamReader(filePath);
                AlgoConfig config = deserializer.Deserialize<AlgoConfig>(reader);

                return config;
            }
        }
    }
}
