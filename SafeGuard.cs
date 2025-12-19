using NinjaTrader.Cbi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinjaTrader.Custom.AddOns.Aurora.SDK.SafeGuard
{
    public static class Guard
    {
        public static T NotNull<T>(T value, string paramName) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
            return value;
        }

        public static IList<T> NotNullList<T>(IList<T> value, string paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
            return value;
        }

        public static void Require(bool condition, string message)
        {
            if (!condition)
                throw new ArgumentException(message);
        }
    }

    public static class Safe
    {
        public static bool TryGetAt(IList<object> values, int index, out object value)
        {
            value = null;
            if (values == null || index < 0 || index >= values.Count)
                return false;

            value = values[index];
            return value != null;
        }

        public static bool TryToMarketPosition(object value, out MarketPosition mp)
        {
            mp = MarketPosition.Flat;
            if (value == null)
                return false;

            if (value is MarketPosition m)
            {
                mp = m;
                return true;
            }

            // Allow int-backed enums or strings if upstream blocks emit them.
            try
            {
                if (value is int i)
                {
                    mp = (MarketPosition)i;
                    return true;
                }

                if (value is string s && Enum.TryParse(s, true, out MarketPosition parsed))
                {
                    mp = parsed;
                    return true;
                }
            }
            catch
            {
                // Intentionally swallow; caller decides behavior.
            }

            return false;
        }

        public static bool TryToBool(object value, out bool b)
        {
            b = false;
            if (value == null)
                return false;

            if (value is bool bb)
            {
                b = bb;
                return true;
            }

            try
            {
                b = Convert.ToBoolean(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryToDouble(object value, out double d)
        {
            d = 0;
            if (value == null)
                return false;

            if (value is double dd)
            {
                d = dd;
                return true;
            }

            try
            {
                d = Convert.ToDouble(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryToInt(object value, out int i)
        {
            i = 0;
            if (value == null)
                return false;

            if (value is int ii)
            {
                i = ii;
                return true;
            }

            try
            {
                i = Convert.ToInt32(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryGetDouble(IDictionary<string, object> dict, string key, out double value)
        {
            value = 0;
            if (dict == null || string.IsNullOrWhiteSpace(key))
                return false;

            if (!dict.TryGetValue(key, out var raw))
                return false;

            return TryToDouble(raw, out value);
        }

        internal static void TryGetDouble(object v, out double acceleration)
        {
            acceleration = 0d;

            if (v == null)
                return;

            switch (v)
            {
                case double d:
                    acceleration = d;
                    return;

                case float f:
                    acceleration = f;
                    return;

                case int i:
                    acceleration = i;
                    return;

                case long l:
                    acceleration = l;
                    return;

                case decimal m:
                    acceleration = (double)m;
                    return;

                case string s:
                    if (double.TryParse(
                          s,
                          System.Globalization.NumberStyles.Any,
                          System.Globalization.CultureInfo.InvariantCulture,
                          out double parsed))
                    {
                        acceleration = parsed;
                        return;
                    }
                    return;

                default:
                    return;
            }
        }
    }
}
