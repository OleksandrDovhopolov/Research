using System.Threading;
using Cysharp.Threading.Tasks;

namespace Infrastructure
{
    public interface IWebClient
    {
        UniTask<TResponse> GetAsync<TResponse>(string url, CancellationToken ct = default);

        UniTask<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default);

        UniTask PostAsync<TRequest>(string url, TRequest data, CancellationToken ct = default);
    }
}
