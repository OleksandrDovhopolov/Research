using System;

namespace core
{
    class EnumTypeParser : ITypeParser
    {
        public ITypeParser FallbackParser { get; set; }

        public object PraseObject(string value, Type objectType, ITypeParser curParser)
        {
            if (!objectType.IsEnum) return FallbackParser.PraseObject(value, objectType, curParser);

            if (string.IsNullOrEmpty(value))
            {
                var values = Enum.GetValues(objectType);

                var curMinIndex = 0;
                var curMinValue = int.MaxValue;

                for (int j = 0; j < values.Length; j++)
                {
                    var val = (int) values.GetValue(j);

                    if (val < curMinValue)
                    {
                        curMinValue = val;
                        curMinIndex = j;
                    }
                }

                return Enum.GetValues(objectType).GetValue(curMinIndex);
            }

            if (!Enum.IsDefined(objectType, value))
            {
                throw new ParsingSkipException($"Value for enum type {objectType.Name} : {value} not defined");
            }

            return Enum.Parse(objectType, value);
        }
    }
}