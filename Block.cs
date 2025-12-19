using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK.Block
{
    public struct SignalContext
    {
        public bool isEntry;
        public SignalOrderTypes Type;
        public MarketPosition Direction;
        public int Size;
        public string Name;
        public double LimitPrice;
        public double StopPrice;
    }

    public enum SignalOrderTypes
    {
        Market,
        Limit,
        StopMarket,
        StopLimit,
    }

    public enum BlockTypes
    {
        Signal,
        Risk,
        Update,
        Execution
    }

    public enum BlockSubTypes
    {
        // Signal Engine
        Signal,
        Bias,
        Filter,

        // Risk Engine
        Multiplier,
        Limit,

        // Update Engine
        BarUpdate,
        ExecutionUpdate,
        OrderUpdate,

        // Execution Engine
        Entry,
        Meta
    }

    public abstract class LogicBlock
    {
        public struct BlockConfig
        {
            public string BlockId;
            public BlockTypes BlockType;
            public BlockSubTypes BlockSubType;
            public Dictionary<string, Type> ParameterList;
        }

        public struct LogicTicket
        {
            public int TicketId;
            public string BlockId;
            public List<object> Values;
        }

        private Strategy _host;

        public bool Initialized { get; private set; } = false;
        public string Id { get; private set; } = null;
        public BlockTypes Type { get; private set; }
        public BlockSubTypes SubType { get; private set; }

        public Dictionary<string, object> Parameters = new();

        internal static void ValidateParameters(Dictionary<string, Type> parameterList, Dictionary<string, object> parameters)
        {
            foreach (var kvp in parameterList)
            {
                var key = kvp.Key;
                var expectedType = kvp.Value;

                if (!parameters.TryGetValue(key, out var value))
                    throw new KeyNotFoundException($"Missing required parameter: {key}");

                if (value == null)
                    throw new InvalidOperationException($"Parameter '{key}' is null");

                var actualType = value.GetType();

                if (!expectedType.IsAssignableFrom(actualType))
                    throw new InvalidOperationException(
                      $"Parameter '{key}' has invalid type. Expected {expectedType.Name}, got {actualType.Name}"
                    );
            }
        }

        protected internal void Initialize(Strategy Host, BlockConfig Config, Dictionary<string, object> Parameters) // must be called from abstracted constructor
        {
            this._host = Host;
            this.Id = Config.BlockId;
            this.Type = Config.BlockType;
            this.SubType = Config.BlockSubType;
            this.Initialized = true;

            ValidateParameters(Config.ParameterList, Parameters);
            this.Parameters = Parameters;
        }

        protected abstract List<object> Forward(Dictionary<string, object> inputs);

        internal LogicTicket SafeGuardForward(Dictionary<string, object> inputs)
        {
            if (this.Initialized == false)
            {
                // TODO: Log
                return new LogicTicket()
                {
                    BlockId = this.Id,
                    Values = []
                };
            }

            List<object> values = new();


            values = this.Forward(inputs);


            return new LogicTicket
            {
                BlockId = this.Id,
                Values = values
            };
        }
    }
}
