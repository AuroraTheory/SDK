using NinjaTrader.Custom.AddOns.Aurora.SDK.Block;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NinjaTrader.Custom.AddOns.Aurora.SDK.AlgoConfig;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK
{
    public static class Helpers
    {
        public static List<LogicBlock> SortLogicBlocks(List<LogicBlock> blocks, BlockTypes type)
        {
            if (blocks == null)
                return [];

            var filtered = blocks
              .Where(b => b.Type == type)
              .ToList();

            filtered = [.. filtered];

            return filtered;
        }

        public static List<LogicBlock> ParseConfigFile(string filePath, HostStrategy host)
        {
            AlgoConfig algoConfig = new();
            LogicBlockFactory blockFactory = new();
            List<LogicBlock> lbs = new();
            try
            {
                algoConfig = LoadConfig(filePath);
                if (algoConfig == null || algoConfig.Logic == null || algoConfig.Logic.Count == 0)
                {
                    //this.ATDebug($"ParseConfigFile: Config is Null", LogMode.Log, LogLevel.Error);
                    throw new NullReferenceException();
                }
                foreach (LogicBlockConfig lbc in algoConfig.Logic)
                {
                    LogicBlock lb = blockFactory.Create(lbc.BID, host, lbc.BParameters);
                    lbs.Add(lb);
                }
            }
            catch (Exception ex)
            {
                lbs = [];
                throw ex;
            }
            return lbs;
        }
    }
}
