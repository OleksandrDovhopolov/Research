using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace core
{
    public class ConfigParam
    {
        private static readonly Regex BooleanTruePattern = new Regex("^(1|true|t|yes|y|on)$", RegexOptions.IgnoreCase);
        private static readonly Regex BooleanFalsePattern = new Regex("^(0|false|f|no|n|off|)$", RegexOptions.IgnoreCase);
        private static readonly Regex ArrayPattern = new Regex(@"\[(?<numbers>(?:[^\]]+)?)\]");
        private static readonly NumberFormatInfo NumberFormatInfo = new NumberFormatInfo{NumberDecimalSeparator = "."};

        private object _objectCached;
        private int? _intCached;
        private float? _floatCached;
        private bool? _boolCached;
        private Enum _enumCached;

        public string ParamName { get; }

        public ConfigParam(string paramName, string paramValue)
        {
            ParamName = paramName;
            StringValue = paramValue;
        }

        public string StringValue { get; }

        public bool IsEmpty => string.IsNullOrEmpty(StringValue);
        
        public int IntValue => _intCached ??= string.IsNullOrEmpty(StringValue) ? 0 : Convert.ToInt32(StringValue);

        public float FloatValue
        {
            get
            {
                if (_floatCached.HasValue)
                {
                    return _floatCached.Value;
                }

                if (string.IsNullOrEmpty(StringValue))
                {
                    _floatCached = 0;
                }
                else
                {
                    var validString = StringValue.Replace(',', '.');
                    _floatCached = Convert.ToSingle(validString, NumberFormatInfo);
                }
               
                return _floatCached.Value;
            }
        }

        public static implicit operator int(ConfigParam param) => param?.IntValue ?? 0;

        public bool BoolValue
        {
            get
            {
                string stringValue = this.StringValue;
                if (_boolCached == null)
                {
                    if (BooleanTruePattern.IsMatch(stringValue))
                        _boolCached = true;
                    if (BooleanFalsePattern.IsMatch(stringValue))
                        _boolCached = false;
                }

                if (_boolCached == null)
                {
                    throw new FormatException($"ConfigValue '{(object)stringValue}' is not a boolean value");
                }

                return _boolCached.Value;
            }
        }

        public T GetEnumValue<T>() where T : Enum
        {
            _enumCached ??= (T)Enum.Parse(TypeOf<T>.Raw, StringValue);
            return (T) _enumCached;
        }
        
        public bool TryGetEnumValue<T>(out T value) where T : struct, Enum
        {
            value = default;
            
            if (_enumCached == null)
            {
                if (Enum.TryParse(StringValue, out value))
                {
                    _enumCached ??= value;
                }
            }

            if (_enumCached != null && _enumCached is T)
            {
                value = (T)_enumCached;
                return true;
            }
            
            return false;
        }

        public bool TryGetArrayValue<T>(out List<T> value)
        {
            value = null;
            if (_objectCached is List<T> list)
            {
                value = list;
                return true;
            }
            
            var result = new List<T>();

            //If string is empty shoud return empty list
            if (!string.IsNullOrEmpty(StringValue))
            {
                var valuesString = string.Empty;

                if (!StringValue.StartsWith('[') && !StringValue.EndsWith(']'))
                {
                    valuesString = StringValue;
                }

                if (string.IsNullOrEmpty(valuesString))
                {
                    if (ArrayPattern.IsMatch(StringValue))
                    {
                        valuesString = ArrayPattern.Match(StringValue).Groups["numbers"].Value;
                    }
                }

                if (string.IsNullOrEmpty(valuesString)) return false;

                var values = valuesString.Split(';');

                var typeConverter = TypeDescriptor.GetConverter(TypeOf<T>.Raw);

                if (typeConverter == null) return false;

                foreach (var arrayValue in values)
                {
                    if (string.IsNullOrEmpty(arrayValue)) continue;
                
                    try
                    {
                        result.Add((T) typeConverter.ConvertFromString(arrayValue));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(new InvalidOperationException($"Cant cast value {arrayValue} to type {TypeOf<T>.Raw}"));
                    }
                }
            }

            _objectCached = result;
            value = result;
            return value.Count > 0;
        }
        
        public List<T> GetArrayValue<T>()
        {
            if (_objectCached != null)
            {
                return (List<T>)_objectCached;
            }
            
            var result = new List<T>();

            //If string is empty shoud return empty list
            if (!string.IsNullOrEmpty(StringValue))
            {
                var valuesString = string.Empty;

                if (!StringValue.StartsWith('[') && !StringValue.EndsWith(']'))
                {
                    valuesString = StringValue;
                }

                if (string.IsNullOrEmpty(valuesString))
                {
                    if (ArrayPattern.IsMatch(StringValue))
                    {
                        valuesString = ArrayPattern.Match(StringValue).Groups["numbers"].Value;
                    }
                }

                if (string.IsNullOrEmpty(valuesString))
                {
                    throw new FormatException($"ConfigValue '{StringValue}' cant be parsed as array");
                }

                var values = valuesString.Split(';');

                var typeConverter = TypeDescriptor.GetConverter(TypeOf<T>.Raw);

                if (typeConverter == null)
                {
                    throw new ArrayTypeMismatchException($"Array elem type {TypeOf<T>.Raw} doesnt have converter");
                }

                foreach (var value in values)
                {
                    if (string.IsNullOrEmpty(value)) continue;
                
                    try
                    {
                        result.Add((T) typeConverter.ConvertFromString(value));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(new InvalidOperationException($"Cant cast value {value} to type {TypeOf<T>.Raw}"));
                    }
                }
            }

            _objectCached = result;

            return result;
        }

        public T JsonValue<T>() where T : class
        {
            _objectCached ??= ConfigSerializer.DeserializeObject<T>(StringValue);

            return (T)_objectCached;
        }
    }
}