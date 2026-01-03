using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace core
{
    public static class Extensions
    {
        public static ValType GetSafe<KeyType, ValType>(this IDictionary<KeyType, ValType> self, KeyType key)
        {
            if (self.ContainsKey(key))
                return self[key];

            return default(ValType);
        }
    }
}