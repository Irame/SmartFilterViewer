using Hjson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SmartFilterViewer
{
    [TypeConverter(typeof(GradientKeyConverter))]
    public struct GradientKey
    {
        public bool IsRelative;
        public double Value;

        public override string ToString()
        {
            if (IsRelative)
                return $"{Value * 100}%";
            else
                return $"{Value}";
        }

        public GradientKey(string str)
        {
            if (str.EndsWith("%"))
            {
                IsRelative = true;
                str = str.Substring(0, str.Length - 1);
            }
            else
                IsRelative = false;

            Value = double.Parse(str);

            if (IsRelative)
                Value /= 100;
        }
    }

    public class SensorSettings
    {
        public string FileName { get; set; }
        public Color? Color { get; set; }
    }

    public class Settings
    {
        public Dictionary<int, SensorSettings> Sensors { get; set; }
        public Dictionary<GradientKey, Color> ColorGradient { get; set; }
        public string Property { get; set; }

        public Settings()
        {
            Sensors = new Dictionary<int, SensorSettings>();
            ColorGradient = new Dictionary<GradientKey, Color>();
        }

        public static Settings FromHJson(string fileName)
        {
            var hJson = HjsonValue.Load(fileName).ToString();
            var result = JsonConvert.DeserializeObject<Settings>(hJson);
            return result;
        }

        public void ToHJson(string fileName)
        {
            HjsonValue.Save(
                JsonValue.Parse(JsonConvert.SerializeObject(this, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })), 
                fileName, 
                new HjsonOptions { EmitRootBraces = true });
        }
    }

    public class GradientKeyConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }
        // Overrides the ConvertFrom method of TypeConverter.
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string str)
            {
                return new GradientKey(str);
            }
            return base.ConvertFrom(context, culture, value);
        }
        // Overrides the ConvertTo method of TypeConverter.
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return ((GradientKey)value).ToString();
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
