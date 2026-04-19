using NinjaTrader.NinjaScript.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK
{
#pragma warning disable CS0436 // Type conflicts with imported type
    public abstract partial class HostStrategy : Strategy
#pragma warning restore CS0436 // Type conflicts with imported type
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
