using System;
using System.Reflection;
using CsvLoader.Editor;
using Newtonsoft.Json;

namespace UGI.MD
{
    public class CsvJsonColumnBinder : IParsingColumnBinder
    {
        public bool HaveBind(string tableFieldName, MemberInfo memberInfo)
        {
            return Attribute.GetCustomAttribute(memberInfo, typeof(JsonPropertyAttribute)) is JsonPropertyAttribute
                jsonPropAttribute && jsonPropAttribute.PropertyName == tableFieldName;
        }
    }
}