using System;
using System.Collections.Generic;
using NinjaTrader.Custom.AddOns.Aurora.SDK.Block;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK
{
    public static class LogicBlockRegistry
    {
        private static readonly Dictionary<string, Func<HostStrategy, Dictionary<string, object>, LogicBlock>> _map = [];

        public static void Register(string blockId, Func<HostStrategy, Dictionary<string, object>, LogicBlock> factory)
        {
            if (_map.ContainsKey(blockId))
                return;

            _map[blockId] = factory;
        }

        public static Func<HostStrategy, Dictionary<string, object>, LogicBlock> Create(string blockId, HostStrategy host, Dictionary<string, object> parameters)
        {
            if (!_map.ContainsKey(blockId))
                throw new InvalidOperationException($"Unknown BID {blockId}");

            return _map[blockId];
        }
    }
}
