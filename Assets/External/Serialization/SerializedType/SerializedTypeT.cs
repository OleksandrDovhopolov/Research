using System;

namespace CsvLoader.Serialization
{
    [Serializable]
    public class SerializedTypeT<T> : SerializedType where T : class
    {
        public T Instantiate()
        {
            return !HaveValue() ? default : (T)Activator.CreateInstance(TypeValue);
        }
    }
}