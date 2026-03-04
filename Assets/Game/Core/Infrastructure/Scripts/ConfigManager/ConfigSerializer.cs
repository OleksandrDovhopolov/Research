using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Infrastructure
{
    public static class ConfigSerializer
    {
        private static readonly Lazy<KnownTypesBinder> TypesBinder = new Lazy<KnownTypesBinder>(() =>
        {
            var binder = new KnownTypesBinder();
            //binder.KnownTypes = ApplicationWrapper.GetGameAssemblyTypes().FindAll(type => type.GetCustomAttribute(TypeOf<NestedConfigJsonModelAttribute>.Raw) != null);
            return binder;
        });

        private static readonly Lazy<JsonSerializerSettings> SerializationSettings = new Lazy<JsonSerializerSettings>(() =>
            new JsonSerializerSettings()
            {
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                TypeNameHandling = TypeNameHandling.All,
                SerializationBinder = TypesBinder.Value
            });


        public static string SerializeObject(object o) =>
            JsonConvert.SerializeObject(o, Formatting.None, SerializationSettings.Value);
        
        [CanBeNull] public static object DeserializeObject(string json) => JsonConvert.DeserializeObject(json, SerializationSettings.Value);

        public static Task<object> DeserializeObjectAsync(string json, CancellationToken cancellationToken) =>
            TaskHelper.DeserializeObjectAsync(json, SerializationSettings.Value, cancellationToken);
        
        [CanBeNull] public static T DeserializeObject<T>(string json) where T : class => JsonConvert.DeserializeObject<T>(json, SerializationSettings.Value);
        
        public class KnownTypesBinder : ISerializationBinder
        {
            public IList<Type> KnownTypes { get; set; }

            public Type BindToType(string assemblyName, string typeName)
            {
                return KnownTypes.SingleOrDefault(t => t.Name == typeName);
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = null;
                typeName = serializedType.Name;
            }
        }
    }
}
