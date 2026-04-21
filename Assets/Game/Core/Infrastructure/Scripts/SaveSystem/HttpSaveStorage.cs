using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Infrastructure
{
    //TODO refactor. the same GET logic in ServerRewardPlayerStateSyncService
    public sealed class HttpSaveStorage : ISaveStorage
    {
        private const string LogPrefix = "[HttpSaveStorage]";

        private readonly string _fullUrl;
        private readonly string _playerId;
        private readonly string _authToken;
        private readonly SemaphoreSlim _ioSemaphore = new(1, 1);

        public HttpSaveStorage(string authToken, IPlayerIdentityProvider playerIdentityProvider)
        {
            if (playerIdentityProvider == null)
            {
                throw new ArgumentNullException(nameof(playerIdentityProvider));
            }

            _fullUrl = ApiConfig.BaseUrl + ApiConfig.SaveGlobalPath;
            _authToken = string.IsNullOrWhiteSpace(authToken) ? null : authToken;
            _playerId = playerIdentityProvider.GetPlayerId();
            if (string.IsNullOrWhiteSpace(_playerId))
            {
                throw new InvalidOperationException("Player identity provider returned an empty player id.");
            }

            Debug.Log(
                $"{LogPrefix} Created. Endpoint={_fullUrl}, PlayerId={MaskPlayerId(_playerId)}, HasAuthToken={!string.IsNullOrEmpty(_authToken)}");
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
                    playerId = _playerId,
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
                var normalizedData = TryExtractDataFromEnvelope(rawResponseText, out var extractionMode);
                Debug.Log(
                    $"{LogPrefix} LoadAsync() completed. ExtractionMode={extractionMode}, " +
                    $"RawLength={(rawResponseText?.Length ?? 0)}, NormalizedLength={(normalizedData?.Length ?? 0)}, " +
                    $"RawPreview={TruncateForLog(rawResponseText)}, NormalizedPreview={TruncateForLog(normalizedData)}");
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
            var separator = _fullUrl.Contains("?") ? "&" : "?";
            var encodedPlayerId = UnityWebRequest.EscapeURL(_playerId);
            return $"{_fullUrl}{separator}playerId={encodedPlayerId}";
        }

        private static string MaskPlayerId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                return "<empty>";
            }

            if (playerId.Length <= 8)
            {
                return playerId;
            }

            return $"{playerId.Substring(0, 4)}...{playerId.Substring(playerId.Length - 4, 4)}";
        }

        private static string TryExtractDataFromEnvelope(string responseText, out string extractionMode)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                extractionMode = "empty";
                return responseText;
            }

            try
            {
                var obj = JObject.Parse(responseText);
                var dataToken = obj["data"];
                if (dataToken?.Type == JTokenType.String)
                {
                    extractionMode = "data-string";
                    return dataToken.Value<string>();
                }

                if (dataToken is { Type: JTokenType.Object or JTokenType.Array })
                {
                    extractionMode = "data-json";
                    return dataToken.ToString();
                }

                extractionMode = "raw-json";
                return responseText;
            }
            catch (Exception)
            {
                // Response can be raw JSON save blob; return as-is.
                extractionMode = "raw-text";
            }

            return responseText;
        }

        private static string TruncateForLog(string value, int maxLength = 320)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "<empty>";
            }

            var normalized = value.Replace("\r", "\\r").Replace("\n", "\\n");
            return normalized.Length <= maxLength
                ? normalized
                : normalized.Substring(0, maxLength) + "...";
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
