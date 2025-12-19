using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK
{
    public abstract partial class HostStrategy : Strategy
    {
        private void SetDefaultsHandler()
        {

        }

        private void ConfigureHandler()
        {
            // Configuration logic can be added here
        }

        private void DataLoadedHandler()
        {
            try
            {
                this.Register();
                List<Block.LogicBlock> _aBlocks = null; //ParseConfigFile(CFGPATH);
                _layer.Initialize(this, _aBlocks);
            }
            catch (Exception ex)
            {
                //ATDebug($"Exception in DataLoadedHandler: {ex.Message}, {ex.StackTrace}", LogMode.Log, LogLevel.Error);
                throw;
            }
            finally
            {
                //ATDebug("AURORA STRATEGY INIT COMPLETE", LogMode.Log, LogLevel.Information);
            }
        }

    }
}
