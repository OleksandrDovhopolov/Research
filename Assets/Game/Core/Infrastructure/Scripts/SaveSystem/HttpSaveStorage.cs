using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Infrastructure
{
    public sealed class HttpSaveStorage : ISaveStorage
    {
        private const string HTTP_SAVE_ENDPOINT = "http://localhost:5000/api/save/global";
        
        private readonly string _authToken;
        private readonly SemaphoreSlim _ioSemaphore = new(1, 1);

        public HttpSaveStorage(string authToken = null)
        {
            _authToken = string.IsNullOrWhiteSpace(authToken) ? null : authToken;
        }

        public bool Exists() => true;

        public async UniTask SaveAsync(string data, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _ioSemaphore.WaitAsync(cancellationToken);
            try
            {
                using var request = new UnityWebRequest(HTTP_SAVE_ENDPOINT, UnityWebRequest.kHttpVerbPUT);
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(data ?? string.Empty));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                ApplyHeaders(request);
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
                ThrowIfFailed(request, "Save");
            }
            finally
            {
                _ioSemaphore.Release();
            }
        }

        public async UniTask<string> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _ioSemaphore.WaitAsync(cancellationToken);
            try
            {
                using var request = UnityWebRequest.Get(HTTP_SAVE_ENDPOINT);
                request.downloadHandler = new DownloadHandlerBuffer();
                ApplyHeaders(request);
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
                if (request.responseCode == 404)
                {
                    return null;
                }

                ThrowIfFailed(request, "Load");
                return request.downloadHandler?.text;
            }
            finally
            {
                _ioSemaphore.Release();
            }
        }

        public async UniTask DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _ioSemaphore.WaitAsync(cancellationToken);
            try
            {
                using var request = UnityWebRequest.Delete(HTTP_SAVE_ENDPOINT);
                request.downloadHandler = new DownloadHandlerBuffer();
                ApplyHeaders(request);
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
                if (request.responseCode == 404)
                {
                    return;
                }

                ThrowIfFailed(request, "Delete");
            }
            finally
            {
                _ioSemaphore.Release();
            }
        }

        public async UniTask<long> GetLastModifiedTimestampAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _ioSemaphore.WaitAsync(cancellationToken);
            try
            {
                using var request = UnityWebRequest.Head(HTTP_SAVE_ENDPOINT);
                ApplyHeaders(request);
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
                if (request.responseCode == 404)
                {
                    return 0;
                }

                ThrowIfFailed(request, "GetLastModifiedTimestamp");

                var unixHeader = request.GetResponseHeader("X-Last-Modified-Unix");
                if (long.TryParse(unixHeader, out var unixSeconds))
                {
                    return unixSeconds;
                }

                var lastModifiedHeader = request.GetResponseHeader("Last-Modified");
                if (DateTimeOffset.TryParse(lastModifiedHeader, out var parsed))
                {
                    return parsed.ToUnixTimeSeconds();
                }

                return 0;
            }
            finally
            {
                _ioSemaphore.Release();
            }
        }

        private void ApplyHeaders(UnityWebRequest request)
        {
            if (!string.IsNullOrEmpty(_authToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
            }
        }

        private static void ThrowIfFailed(UnityWebRequest request, string operationName)
        {
            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            {
                var body = request.downloadHandler?.text;
                throw new InvalidOperationException(
                    $"[HttpSaveStorage] {operationName} failed. Status={(int)request.responseCode}, Error={request.error}, Body={body}");
            }
        }
    }
}
