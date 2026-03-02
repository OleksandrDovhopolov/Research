using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace core
{
    public static class TaskHelper
    {
        public static Task<T> DeserializeObjectAsync<T>(string json, JsonSerializerSettings settings) =>
            Task.Run(() => JsonConvert.DeserializeObject<T>(json, settings));
        
        public static Task<T> DeserializeObjectAsync<T>(string json, JsonSerializerSettings settings, CancellationToken cancellationToken) =>
            Task.Run(() => JsonConvert.DeserializeObject<T>(json, settings), cancellationToken);

        public static Task<object> DeserializeObjectAsync(string json, JsonSerializerSettings settings,
            CancellationToken cancellationToken) => Task.Run(() => JsonConvert.DeserializeObject(json, settings), cancellationToken);
    }
}