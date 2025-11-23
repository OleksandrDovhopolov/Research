using System;

namespace CsvLoader.Serialization
{
    public class SerializedTypeRestrictionAttribute : Attribute
    {
        public readonly Type Type;

        public SerializedTypeRestrictionAttribute(Type type)
        {
            Type = type;
        }
    }
}