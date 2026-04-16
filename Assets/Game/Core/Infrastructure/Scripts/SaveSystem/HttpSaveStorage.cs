using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Infrastructure
{
    public sealed class HttpSaveStorage : ISaveStorage
    {
        public static string TemporaryPlayerId = "local-dev-player";
        public static string BaseUrl = "http://localhost:5000/api/v1/";
        private const string SaveUrl = "save/global";
        private string FullUrl = BaseUrl + SaveUrl;
        
        
        //private const string HTTP_SAVE_ENDPOINT = "http://localhost:5000/api/save/global";
        private const string LogPrefix = "[HttpSaveStorage]";
        
        private readonly string _authToken;
        private readonly SemaphoreSlim _ioSemaphore = new(1, 1);

        public HttpSaveStorage(string authToken = null)
        {
            _authToken = string.IsNullOrWhiteSpace(authToken) ? null : authToken;
            Debug.Log($"{LogPrefix} Created. Endpoint={FullUrl}, PlayerId={TemporaryPlayerId}, HasAuthToken={!string.IsNullOrEmpty(_authToken)}");
        }

        public bool Exists()
        {
            Debug.Log($"{LogPrefix} Exists() called -> true (remote storage)");
            return true;
        }

        public async UniTask SaveAsync(string data, CancellationToken cancellationToken)
        {
            var url = BuildUrlWithPlayerId();
            Debug.Log($"{LogPrefix} SaveAsync() called. Url={url}, PayloadLength={(data?.Length ?? 0)}");
            cancellationToken.ThrowIfCancellationRequested();
            await _ioSemaphore.WaitAsync(cancellationToken);
            try
            {
                var requestBody = JsonUtility.ToJson(new SaveRequestPayload
                {
                    playerId = TemporaryPlayerId,
                    data = data ?? string.Empty
                });

                await SendSaveRequestAsync(url, UnityWebRequest.kHttpVerbPOST, requestBody, cancellationToken);

                Debug.Log($"{LogPrefix} SaveAsync() completed successfully.");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"{LogPrefix} SaveAsync() canceled.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} SaveAsync() failed: {ex}");
                throw;
            }
            finally
            {
                _ioSemaphore.Release();
            }
        }

        private async UniTask SendSaveRequestAsync(string url, string method, string requestBody, CancellationToken cancellationToken)
        {
            using var request = new UnityWebRequest(url, method);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            ApplyHeaders(request);
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            Debug.Log($"{LogPrefix} SaveAsync({method}) response. Status={(int)request.responseCode}, Result={request.result}, Error={request.error}");
            ThrowIfFailed(request, "Save");
        }

        public async UniTask<string> LoadAsync(CancellationToken cancellationToken)
        {
            var url = BuildUrlWithPlayerId();
            Debug.Log($"{LogPrefix} LoadAsync() called. Url={url}");
            cancellationToken.ThrowIfCancellationRequested();
            await _ioSemaphore.WaitAsync(cancellationToken);
            try
            {
                using var request = UnityWebRequest.Get(url);
                request.downloadHandler = new DownloadHandlerBuffer();
                ApplyHeaders(request);
                try
                {
                    await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
                }
                catch (UnityWebRequestException ex) when (ex.ResponseCode == 404)
                {
                    Debug.Log($"{LogPrefix} LoadAsync() no remote save found (404 via exception). Returning null.");
                    return null;
                }

                Debug.Log($"{LogPrefix} LoadAsync() response. Status={(int)request.responseCode}, Result={request.result}, Error={request.error}");
                if (request.responseCode == 404)
                {
                    Debug.Log($"{LogPrefix} LoadAsync() no remote save found (404). Returning null.");
                    return null;
                }

                ThrowIfFailed(request, "Load");
                var rawResponseText = request.downloadHandler?.text;
                var normalizedData = TryExtractDataFromEnvelope(rawResponseText);
                Debug.Log($"{LogPrefix} LoadAsync() completed. RawLength={(rawResponseText?.Length ?? 0)}, NormalizedLength={(normalizedData?.Length ?? 0)}");
                return normalizedData;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"{LogPrefix} LoadAsync() canceled.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} LoadAsync() failed: {ex}");
                throw;
            }
            finally
            {
                _ioSemaphore.Release();
            }
        }

        public async UniTask DeleteAsync(CancellationToken cancellationToken)
        {
            var url = BuildUrlWithPlayerId();
            Debug.Log($"{LogPrefix} DeleteAsync() called. Url={url}");
            cancellationToken.ThrowIfCancellationRequested();
            await _ioSemaphore.WaitAsync(cancellationToken);
            try
            {
                using var request = UnityWebRequest.Delete(url);
                request.downloadHandler = new DownloadHandlerBuffer();
                ApplyHeaders(request);
                try
                {
                    await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
                }
                catch (UnityWebRequestException ex) when (ex.ResponseCode == 404)
                {
                    Debug.Log($"{LogPrefix} DeleteAsync() remote save not found (404 via exception).");
                    return;
                }

                Debug.Log($"{LogPrefix} DeleteAsync() response. Status={(int)request.responseCode}, Result={request.result}, Error={request.error}");
                if (request.responseCode == 404)
                {
                    Debug.Log($"{LogPrefix} DeleteAsync() remote save not found (404).");
                    return;
                }

                ThrowIfFailed(request, "Delete");
                Debug.Log($"{LogPrefix} DeleteAsync() completed successfully.");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"{LogPrefix} DeleteAsync() canceled.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} DeleteAsync() failed: {ex}");
                throw;
            }
            finally
            {
                _ioSemaphore.Release();
            }
        }

        public async UniTask<long> GetLastModifiedTimestampAsync(CancellationToken cancellationToken)
        {
            var url = BuildUrlWithPlayerId();
            Debug.Log($"{LogPrefix} GetLastModifiedTimestampAsync() called. Url={url}");
            cancellationToken.ThrowIfCancellationRequested();
            await _ioSemaphore.WaitAsync(cancellationToken);
            try
            {
                using var request = UnityWebRequest.Head(url);
                ApplyHeaders(request);
                try
                {
                    await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
                }
                catch (UnityWebRequestException ex) when (ex.ResponseCode == 404)
                {
                    Debug.Log($"{LogPrefix} GetLastModifiedTimestampAsync() no remote save found (404 via exception). Returning 0.");
                    return 0;
                }

                Debug.Log($"{LogPrefix} GetLastModifiedTimestampAsync() response. Status={(int)request.responseCode}, Result={request.result}, Error={request.error}");
                if (request.responseCode == 404)
                {
                    Debug.Log($"{LogPrefix} GetLastModifiedTimestampAsync() no remote save found (404). Returning 0.");
                    return 0;
                }

                ThrowIfFailed(request, "GetLastModifiedTimestamp");

                var unixHeader = request.GetResponseHeader("X-Last-Modified-Unix");
                if (long.TryParse(unixHeader, out var unixSeconds))
                {
                    Debug.Log($"{LogPrefix} GetLastModifiedTimestampAsync() parsed X-Last-Modified-Unix={unixSeconds}.");
                    return unixSeconds;
                }

                var lastModifiedHeader = request.GetResponseHeader("Last-Modified");
                if (DateTimeOffset.TryParse(lastModifiedHeader, out var parsed))
                {
                    var parsedUnix = parsed.ToUnixTimeSeconds();
                    Debug.Log($"{LogPrefix} GetLastModifiedTimestampAsync() parsed Last-Modified={parsedUnix}.");
                    return parsedUnix;
                }

                Debug.LogWarning($"{LogPrefix} GetLastModifiedTimestampAsync() headers missing/unparseable. Returning 0.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"{LogPrefix} GetLastModifiedTimestampAsync() canceled.");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} GetLastModifiedTimestampAsync() failed: {ex}");
                throw;
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
                Debug.Log($"{LogPrefix} Authorization header applied.");
            }
        }

        private string BuildUrlWithPlayerId()
        {
            var separator = FullUrl.Contains("?") ? "&" : "?";
            var encodedPlayerId = UnityWebRequest.EscapeURL(TemporaryPlayerId ?? string.Empty);
            return $"{FullUrl}{separator}playerId={encodedPlayerId}";
        }

        private static string TryExtractDataFromEnvelope(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return responseText;
            }

            try
            {
                var obj = JObject.Parse(responseText);
                var dataToken = obj["data"];
                if (dataToken?.Type == JTokenType.String)
                {
                    return dataToken.Value<string>();
                }
            }
            catch (Exception)
            {
                // Response can be raw JSON save blob; return as-is.
            }

            return responseText;
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

        [Serializable]
        private sealed class SaveRequestPayload
        {
            public string playerId;
            public string data;
        }
    }
}
