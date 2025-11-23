using System;

namespace core
{
    public class StringTypeParser : ITypeParser
    {
        public ITypeParser FallbackParser { get; set; }
        
        public object PraseObject(string value, Type objectType, ITypeParser curParser)
        {
            return objectType == typeof(string) && string.IsNullOrEmpty(value)
                ? null
                : FallbackParser.PraseObject(value, objectType, curParser);
        }
    }
}