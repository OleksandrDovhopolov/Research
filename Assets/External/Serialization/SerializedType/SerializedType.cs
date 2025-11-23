using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CsvLoader.Serialization
{
    /// <summary>
    /// Wrapper for serializing types for runtime.
    /// </summary>
    [Serializable]
    public class SerializedType
    {
        public const string NullName = "null";
        
        [field: SerializeField] public string AssemblyName { get; private set; }
        [field: SerializeField] public string ClassName { get; private set; }

        private Type _cachedType;

        public override string ToString()
        {
            return TypeValue == null ? NullName : TypeValue.Name;
        }

        public bool HaveValue() => !string.IsNullOrEmpty(AssemblyName) && !string.IsNullOrEmpty(ClassName);

        /// <summary>
        /// Get and set the serialized type.
        /// </summary>
        public Type TypeValue
        {
            get
            {
                try
                {
                    if (!HaveValue())
                        return null;

                    if (_cachedType == null)
                    {
                        var assembly = Assembly.Load(AssemblyName);
                        if (assembly != null)
                            _cachedType = assembly.GetType(ClassName);
                    }
                    return _cachedType;
                }
                catch (Exception ex)
                {
                    //file not found is most likely an editor only type, we can ignore error.
                    if (ex.GetType() != typeof(FileNotFoundException))
                        Debug.LogException(ex);
                    return null;
                }
            }
            set
            {
                if (value != null)
                {
                    AssemblyName = value.Assembly.FullName;
                    ClassName = value.FullName;
                }
                else
                {
                    AssemblyName = ClassName = null;
                }
            }
        }

        /// <summary>
        /// Used for multi-object editing. Indicates whether or not property value was changed.
        /// </summary>
        public bool ValueChanged { get; set; }
    }
}
