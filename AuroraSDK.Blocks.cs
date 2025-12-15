#region Definitions
using NinjaTrader.Cbi;
using NinjaTrader.CQG.ProtoBuf;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using static NinjaTrader.Custom.Strategies.Aurora.SDK.AuroraStrategy;
#endregion


namespace NinjaTrader.Custom.Strategies.Aurora.SDK
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
            Limit,
            Extra,
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
            public List<object> Values;
        }

        public abstract class LogicBlock
        {
            internal AuroraStrategy _host;
            public Dictionary<string, object> Parameters { get; private set; }
            public List<int> DataIds { get; private set; }
            public Type TicketDataType { get; private set; }
            public int Id { get; private set; }
            public BlockTypes Type { get; private set; }
            public BlockSubTypes SubType { get; private set; }

            protected internal void Initialize(AuroraStrategy Host, BlockConfig Config) // must be called from abstracted constructor
            {
                this._host = Host;
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
