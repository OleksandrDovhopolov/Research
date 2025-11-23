using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CsvLoader.Serialization
{
    [Serializable]
    public class SerializedDictionary<TKey, TValue> : SerializedDictionaryDrawable, IReadOnlyDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        private static readonly Dictionary<TKey, TValue> Empty = new(0);
        private Dictionary<TKey, TValue> _dict;

        public TValue this[TKey key]
        {
            get
            {
                if (_dict == null) throw new KeyNotFoundException();
                return _dict[key];
            }
            set
            {
                _dict ??= new Dictionary<TKey, TValue>();
                _dict[key] = value;
            }
        }

        public IEnumerable<TKey> Keys => (_dict ??= new Dictionary<TKey, TValue>()).Keys;
        public IEnumerable<TValue> Values => (_dict ??= new Dictionary<TKey, TValue>()).Values;
        
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return (_dict ?? Empty).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _dict?.Count ?? 0;

        public bool ContainsKey(TKey key)
        {
            if (_dict == null)
                return false;

            return _dict.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_dict == null)
            {
                value = default(TValue);
                return false;
            }

            return _dict.TryGetValue(key, out value);
        }
        
        /// <summary>
        /// Returns true if the value exists; otherwise, false
        /// </summary>
        /// <param name="value">Value to check</param>
        public bool ContainsValue(TValue value)
        {
            if (_dict == null)
                return false;

            return _dict.ContainsValue(value);
        }

        public bool RemoveElement(TKey key)
        {
            if (_dict == null)
                return false;
            
            return _dict.Remove(key);
        }

        #region ISerializationCallbackReceiver

        [Serializable]
        public class SerializedDictionaryKeyValue
        {
            public TKey Key => _key;
            [SerializeField] private TKey _key;

            public TValue Value => _value;
            [SerializeField] private TValue _value;

            public SerializedDictionaryKeyValue(TKey key, TValue value)
            {
                _key = key;
                _value = value;
            }
        }

        [SerializeField] private List<SerializedDictionaryKeyValue> _elements;
        
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (_elements == null) return;
            
            _dict ??= new Dictionary<TKey, TValue>(_elements.Count);
            _dict.Clear();

            foreach (var dictionaryKeyValue in _elements)
            {
                if (!_dict.ContainsKey(dictionaryKeyValue.Key))
                {
                    _dict.Add(dictionaryKeyValue.Key, dictionaryKeyValue.Value);
                }
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
        }

        #endregion
    }
}