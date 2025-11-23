using System;
using System.Collections;
using System.Linq;

namespace core
{
    class ListTypeParser : ITypeParser
    {
        public ITypeParser FallbackParser { get; set; }
        
        public object PraseObject(string value, Type objectType, ITypeParser curParser)
        {
            if (!objectType.GetInterfaces().Contains(typeof(IList)))
            {
                return FallbackParser.PraseObject(value, objectType, curParser);
            }
            
            var list = (IList)Activator.CreateInstance(objectType);

            var items = value.Split(new char[] { '[', ';', ']' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in items)
            {
                list.Add(curParser.PraseObject(item, objectType.GenericTypeArguments[0], curParser));
            }

            return list;
        }
    }
}