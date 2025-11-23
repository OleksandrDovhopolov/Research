using System;
using CsvLoader.Editor;

namespace core
{
    public class TypeParserBase : ITypeParser
    {
        public ITypeParser FallbackParser { get; set; }
        
        public object PraseObject(string value, Type objectType, ITypeParser curParser)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Activator.CreateInstance(objectType);
            }

            return Convert.ChangeType(value, objectType);
        }
    }
}