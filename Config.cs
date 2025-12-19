using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK
{
    public sealed class AlgoConfig 
    {
        public List<LogicBlockConfig> Logic { get; set; }

        public sealed class LogicBlockConfig
        {
            public string BID { get; set; }
            public string BType { get; set; }
            public string BSubType { get; set; }

            public Dictionary<string, object> BParameters { get; set; }
            public DebugConfig Debug { get; set; }

            public sealed class DebugConfig
            {
                public bool Enabled { get; set; }
                public string Type { get; set; }
            }

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
                  $"BType={BType}, " +
                  $"BSubType={BSubType}, " +
                  $"BParameters={parametersStr}, " +
                  $"Debug={Debug}" +
                  $")";
            }
        }

        public override string ToString()
        {
            if (Logic == null || Logic.Count == 0)
                return "AlgoConfig { Logic=[] }";

            return $"AlgoConfig {{ LogicCount={Logic.Count}, Logic=[{string.Join("; ", Logic)}] }}";
        }

        public static AlgoConfig LoadConfig(string filePath)
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