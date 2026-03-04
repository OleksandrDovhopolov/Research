using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Infrastructure
{
    public static class BinarySaverBytes
    {
        public static Task<byte[]> SaveDataAsync(object data)
        {
            return Task.Run(() =>
            {
                try
                {
                    return Task.FromResult(SaveData(data));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    return Task.FromResult<byte[]>(default);
                }
            });
        }

        public static byte[] SaveData(object data)
        {
            var binaryFormatter = GetBinaryFormatter();
            
            using (var memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, data);
                return memoryStream.ToArray();
            }
        }
        
        public static T LoadData<T>(byte[] data)
        {
            if (data == null) return default;
            
            var binaryFormatter = GetBinaryFormatter();
            T result = default;

            try
            {
                using (var memoryStream = new MemoryStream(data))
                {
                    result = (T)binaryFormatter.Deserialize(memoryStream);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            
            return result;
        }
        
        public static Task<T> LoadDataAsync<T>(byte[] data)
        {
            if (data == null) 
                return Task.FromResult<T>(default);
            
            Debug.Log($"Start deserialize {data.GetHashCode()} {TypeOf<T>.Raw}");

            return Task.Run(() =>
            {
                var binaryFormatter = GetBinaryFormatter();
                
                try
                {
                    using (var memoryStream = new MemoryStream(data))
                    {
                        var result = (T)binaryFormatter.Deserialize(memoryStream);
                        return Task.FromResult(result);
                    }
                }
                catch (Exception e)
                {            
                    Debug.LogError($"Error deserialize {data.GetHashCode()} {TypeOf<T>.Raw}");
                    Debug.LogException(e);
                    return Task.FromResult<T>(default);
                }
            });
        }

        public static Task<object> LoadDataAsync(byte[] data)
        {
            return LoadDataAsync<object>(data);
        }

        public static string ComputeHash(byte[] objectAsBytes)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            try
            {
                byte[] result = md5.ComputeHash(objectAsBytes);

                // Build the final string by converting each byte
                // into hex and appending it to a StringBuilder
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < result.Length; i++)
                {
                    sb.Append(result[i].ToString("X2"));
                }

                // And return it
                return sb.ToString();
            }
            catch (ArgumentNullException ane)
            {
                //If something occurred during serialization, 
                //this method is called with a null argument. 
                Console.WriteLine("Hash has not been generated.");
                return null;
            }
        }

        static BinaryFormatter GetBinaryFormatter()
        {
            var bf = new BinaryFormatter();
            
            var surrogateSelector = new SurrogateSelector();
            
            surrogateSelector.AddSurrogate(TypeOf<Vector3Int>.Raw, new StreamingContext(StreamingContextStates.All), new Vector3IntSurrogateSerialize());
            surrogateSelector.AddSurrogate(TypeOf<Vector3>.Raw, new StreamingContext(StreamingContextStates.All), new Vector3SurrogateSerialize());
            surrogateSelector.AddSurrogate(TypeOf<Vector2>.Raw, new StreamingContext(StreamingContextStates.All), new Vector2SurrogateSerialize());
            
            bf.SurrogateSelector = surrogateSelector;

            return bf;
        }
    }
}
