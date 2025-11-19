using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AddOns.Aurora.SDK
{
    public enum BlockTypes
    {
        Signal,
        Risk,
    };

    public enum BlockSubTypes
    {
        Bias,
        Filter,
        Multiplier,
        Limit
    };

    public struct BlockConfig
    {
        public int BlockId;
        public List<int> DataIds;
        public BlockTypes BlockType;
        public Type DataType;
        public BlockSubTypes BlockSubType;
    }

    public struct LogicTicket
    {
        public int Id;
        public BlockTypes Type;
        public BlockSubTypes SubType;
        public bool Value;
    }

    public abstract class LogicBlock
    {
        private StrategyBase _host;

        public List<int> DataIds { get; private set; }
        public Type DataType { get; private set; }
        public int Id { get; private set; }
        public BlockTypes Type { get; private set; }
        public BlockSubTypes SubType { get; private set; }

        protected internal void Initialize(StrategyBase Host, BlockConfig Config) // must be called from abstracted constructor
        {
            this._host = Host;
            this.Type = Config.BlockType;
            this.SubType = Config.BlockSubType;
            this.DataType = Config.DataType;
            this.DataIds = Config.DataIds;
        }

        public abstract LogicTicket Forward();
    }
}

