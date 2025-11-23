using System;
using System.Reflection;

namespace core
{
    public static class TypeOf<T>
    {
        public static readonly Type Raw = typeof(T);
	
        public static readonly string Name = Raw.Name;
        public static readonly Assembly Assembly = Raw.Assembly;
        public static readonly bool IsValueType = Raw.IsValueType;
    }
}