#region Definitions
using System;
using System.Text;
using System.Collections.Generic;
using NinjaTrader.Cbi;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.NinjaScript.Indicators;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using System.Diagnostics;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Controls;
using NinjaTrader.CQG.ProtoBuf;
using System.Linq;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
#endregion


namespace NinjaTrader.Custom.AddOns.Aurora.SDK
{
    public abstract partial class AuroraStrategy : Strategy
    {
        public enum BlockTypes
        {
            Signal,
            Update,
            Risk,
            Execution
        }

        public enum BlockSubTypes
        {
            Bias,
            Filter,
            Regime, // Future implementation, just an idea for now
            Multiplier,
            Limit
        }

        public struct BlockConfig
        {
            public int BlockId;
            public List<int> DataIds; // wtf is this used for
            public BlockTypes BlockType;
            public Type TicketDataType;
            public BlockSubTypes BlockSubType;
            public Dictionary<string, object> Parameters;
        }

        public struct LogicTicket
        {
            public int TicketId;
            public int BlockId;
            public Type DataType;
            public object Value;
        }

        public abstract class LogicBlock
        {
            internal StrategyBase _host;
            internal Strategy _strategy;
            public Dictionary<string, object> Parameters { get; private set; }
            public List<int> DataIds { get; private set; }
            public Type TicketDataType { get; private set; }
            public int Id { get; private set; }
            public BlockTypes Type { get; private set; }
            public BlockSubTypes SubType { get; private set; }

            protected internal void Initialize(StrategyBase Host, Strategy Strategy, BlockConfig Config) // must be called from abstracted constructor
            {
                this._host = Host;
                this._strategy = Strategy;
                this.Id = Config.BlockId;
                this.Type = Config.BlockType;
                this.SubType = Config.BlockSubType;
                this.TicketDataType = Config.TicketDataType;
                this.DataIds = Config.DataIds;
                this.Parameters = Config.Parameters;
            }

            public abstract LogicTicket Forward();
        }
    }
}
