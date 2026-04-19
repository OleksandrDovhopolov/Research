using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Infrastructure
{
    public sealed class UnityWebRequestWebClient : IWebClient
    {
        private readonly WebClientOptions _options;
        private readonly IAuthTokenProvider _authTokenProvider;

        public UnityWebRequestWebClient(WebClientOptions options, IAuthTokenProvider authTokenProvider = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                throw new ArgumentException("WebClient BaseUrl is empty.", nameof(options));
            }

            _authTokenProvider = authTokenProvider;
        }

        public async UniTask<TResponse> GetAsync<TResponse>(string url, CancellationToken ct = default)
        {
            using var request = UnityWebRequest.Get(BuildAbsoluteUrl(_options.BaseUrl, url));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = Math.Max(1, _options.TimeoutSeconds);
            await ApplyHeadersAsync(request, ct);

            var responseText = await SendAsync(request, ct);
            return DeserializeResponse<TResponse>(responseText, request.url);
        }

        public async UniTask<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest data, CancellationToken ct = default)
        {
            using var request = CreatePostRequest(url, data);
            var responseText = await SendAsync(request, ct);
            return DeserializeResponse<TResponse>(responseText, request.url);
        }

        public async UniTask PostAsync<TRequest>(string url, TRequest data, CancellationToken ct = default)
        {
            using var request = CreatePostRequest(url, data);
            await SendAsync(request, ct);
        }

        private UnityWebRequest CreatePostRequest<TRequest>(string relativeUrl, TRequest data)
        {
            var requestUrl = BuildAbsoluteUrl(_options.BaseUrl, relativeUrl);
            var payload = JsonConvert.SerializeObject(data);

            var request = new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = Math.Max(1, _options.TimeoutSeconds);
            request.SetRequestHeader("Content-Type", "application/json");

            return request;
        }

        private async UniTask<string> SendAsync(UnityWebRequest request, CancellationToken ct)
        {
            try
            {
                await ApplyHeadersAsync(request, ct);
                await request.SendWebRequest().ToUniTask(cancellationToken: ct);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new WebClientNetworkException(request.url, "Request timeout.");
            }
            catch (UnityWebRequestException exception)
            {
                if (exception.ResponseCode == 401)
                {
                    var unauthorizedBody = request.downloadHandler?.text;
                    Debug.LogError($"[WebClient] 401 Url={request.url}, Body={unauthorizedBody}");
                    throw new WebClientUnauthorizedException(request.url, exception.ResponseCode, unauthorizedBody);
                }

                var body = request.downloadHandler?.text;
                if (exception.ResponseCode >= 400 && exception.ResponseCode <= 599)
                {
                    Debug.LogError($"[WebClient] HTTP error Url={request.url}, Status={exception.ResponseCode}, Body={body}");
                    throw new WebClientHttpException(request.url, exception.ResponseCode, body);
                }

                throw new WebClientNetworkException(request.url, exception.Message, exception);
            }
            catch (Exception exception)
            {
                throw new WebClientNetworkException(request.url, exception.Message, exception);
            }

            var responseBody = request.downloadHandler?.text;
            if (request.responseCode == 401)
            {
                Debug.LogError($"[WebClient] 401 Url={request.url}, Body={responseBody}");
                throw new WebClientUnauthorizedException(request.url, request.responseCode, responseBody);
            }

            if (request.responseCode is < 200 or >= 300)
            {
                Debug.LogError($"[WebClient] HTTP error Url={request.url}, Status={request.responseCode}, Body={responseBody}");
                throw new WebClientHttpException(request.url, request.responseCode, responseBody);
            }

            return responseBody;
        }

        private async UniTask ApplyHeadersAsync(UnityWebRequest request, CancellationToken ct)
        {
            if (_options.DefaultHeaders != null)
            {
                foreach (var header in _options.DefaultHeaders)
                {
                    if (!string.IsNullOrWhiteSpace(header.Key) && !string.IsNullOrWhiteSpace(header.Value))
                    {
                        request.SetRequestHeader(header.Key, header.Value);
                    }
                }
            }

            if (_authTokenProvider == null)
            {
                return;
            }

            var token = await _authTokenProvider.GetTokenAsync(ct);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            }
        }

        private static TResponse DeserializeResponse<TResponse>(string responseText, string requestUrl)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return default;
            }

            try
            {
                return JsonConvert.DeserializeObject<TResponse>(responseText);
            }
            catch (Exception exception)
            {
                throw new WebClientException($"Failed to deserialize response. Url={requestUrl}, Body={responseText}", exception);
            }
        }

        public static string BuildAbsoluteUrl(string baseUrl, string relativeOrAbsoluteUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("BaseUrl is empty.", nameof(baseUrl));
            }

            if (string.IsNullOrWhiteSpace(relativeOrAbsoluteUrl))
            {
                throw new ArgumentException("Request url is empty.", nameof(relativeOrAbsoluteUrl));
            }

            if (Uri.TryCreate(relativeOrAbsoluteUrl, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }

            var baseUri = baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : $"{baseUrl}/";
            var relative = relativeOrAbsoluteUrl.StartsWith("/", StringComparison.Ordinal)
                ? relativeOrAbsoluteUrl.Substring(1)
                : relativeOrAbsoluteUrl;
            return new Uri(new Uri(baseUri), relative).ToString();
        }
    }
}
