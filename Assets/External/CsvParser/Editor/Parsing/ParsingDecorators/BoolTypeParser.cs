using System;

namespace core
{
    class BoolTypeParser : ITypeParser
    {
        public ITypeParser FallbackParser { get; set; }
        
        public object PraseObject(string value, Type objectType, ITypeParser curParser)
        {
            if (objectType != typeof(bool) || string.IsNullOrEmpty(value))
            {
                return FallbackParser.PraseObject(value, objectType, curParser);
            }
            
            return Convert.ToBoolean(Convert.ToInt16(value));
        }
    }
}