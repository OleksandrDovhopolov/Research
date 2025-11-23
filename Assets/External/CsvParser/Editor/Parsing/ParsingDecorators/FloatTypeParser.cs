using System;
using System.Globalization;

namespace core
{
    class FloatTypeParser : ITypeParser
    {
        public ITypeParser FallbackParser { get; set; }
        
        public object PraseObject(string value, Type objectType, ITypeParser curParser)
        {
            if (objectType != typeof(float) || string.IsNullOrEmpty(value))
                return FallbackParser.PraseObject(value, objectType, curParser);

            var separator = CultureInfo.CurrentCulture.NumberFormat.PercentDecimalSeparator;
            
            if (value.Contains(","))
            {
                value = value.Replace(",", separator);
            }

            if (value.Contains("."))
            {
                value = value.Replace(".", separator);
            }

            return (float)decimal.Round(decimal.Parse(value, CultureInfo.CurrentCulture), 4);
        }
    }
}