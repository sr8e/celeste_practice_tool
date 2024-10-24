using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Media;

namespace celeste_practice_tool
{
    public class GraphWidthConverter : IValueConverter
    {
        public object Convert(object value, Type type, object param, CultureInfo culture)
        {
            if (value.GetType() == typeof(float))
            {
                float fvalue = (float)value;
                if ((string)param == "left")
                {
                    return $"{fvalue:.00}*";
                }
                else if ((string)param == "right")
                {
                    return $"{1 - fvalue:.00}*";
                }
                else
                {
                    throw new ArgumentException("invalid parameter");
                }
            }
            else
            {
                throw new ArgumentException("invalid type");
            }
        }

        public object ConvertBack(object value, Type type, object param, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class GraphColorConverter : IValueConverter
    {
        public object Convert(object value, Type type, object param, CultureInfo culture)
        {
            if (value.GetType() == typeof(float))
            {
                float fvalue = (float)value;

                //hsv color space
                float v = 1;
                float s = .7f;
                float h = fvalue * 2;

                float min = v * (1 - s);
                float hres = h - float.Floor(h);
                float x0 = v * (1 - hres * s);

                return new SolidColorBrush((int)h switch
                {
                    0 => Color.FromRgb(denorm(v), denorm(min - x0), denorm(min)),
                    1 => Color.FromRgb(denorm(x0), denorm(v), denorm(min)),
                    2 => Color.FromRgb(denorm(min), denorm(x0), denorm(min - x0)),
                    _ => Color.FromRgb(0xff, 0xff, 0xff)
                });
            }
            else
            {
                throw new ArgumentException("invalid type");
            }
        }
        public object ConvertBack(object value, Type type, object param, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static byte denorm(float value)
        {
            return byte.Max(byte.MinValue, byte.Min(byte.MaxValue, (byte)(value * 0xff)));
        }
    }

    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type type, object param, CultureInfo culture)
        {
            if (value.GetType() == typeof(float))
            {
                float fvalue = (float)value * 100;
                return $"{fvalue:0.0}%";
            }
            else
            {
                throw new ArgumentException("invalid type");
            }
        }
        public object ConvertBack(object value, Type type, object param, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumNameConverter : IValueConverter
    {
        public object Convert(object value, Type type, object param, CultureInfo culture)
        {
            Type srcType = value.GetType();
            if (!srcType.IsEnum)
            {
                throw new ArgumentException("invalid type");
            }
            if (!Enum.IsDefined(srcType, value))
            {
                return "-";
            }
            string name = Enum.GetName(srcType, value);
            Attribute attr = srcType.GetField(name).GetCustomAttribute(typeof(DescriptionAttribute));
            if (attr != null)
            {
                return ((DescriptionAttribute)attr).Description;
            }
            return name;

        }
        public object ConvertBack(object value, Type type, object param, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TimerConverter : IValueConverter
    {
        public object Convert(object value, Type type, object param, CultureInfo culture)
        {
            if (value.GetType() != typeof(long))
            {
                throw new ArgumentException("invalid type");
            }
            long lvalue = long.Max(0, (long)value);
            TimeSpan t = TimeSpan.FromMilliseconds(lvalue);
            if (t.Hours > 0)
            {
                return $"{t.Hours}:{t.Minutes:d2}:{t.Seconds:d2}.{t.Milliseconds:d3}";
            }
            else
            {
                return $"{t.Minutes}:{t.Seconds:d2}.{t.Milliseconds:d3}";
            }

        }
        public object ConvertBack(object value, Type type, object param, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class NullableStringConverter : IValueConverter
    {
        public object Convert(object value, Type type, object param, CultureInfo culture)
        {
            return value?.ToString() ?? "-";
        }
        public object ConvertBack(object value, Type type, object param, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
