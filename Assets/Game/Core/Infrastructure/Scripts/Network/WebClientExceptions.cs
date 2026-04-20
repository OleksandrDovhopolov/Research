using System;

namespace Infrastructure
{
    public class WebClientException : Exception
    {
        public WebClientException(string message, Exception innerException = null) : base(message, innerException)
        {
        }
    }

    public sealed class WebClientUnauthorizedException : WebClientException
    {
        public string Url { get; }
        public long StatusCode { get; }
        public string ResponseBody { get; }

        public WebClientUnauthorizedException(string url, long statusCode, string responseBody)
            : base($"Unauthorized request. Url={url}, Status={statusCode}, Body={responseBody}")
        {
            Url = url;
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }

    public sealed class WebClientHttpException : WebClientException
    {
        public string Url { get; }
        public long StatusCode { get; }
        public string ResponseBody { get; }

        public WebClientHttpException(string url, long statusCode, string responseBody)
            : base($"HTTP request failed. Url={url}, Status={statusCode}, Body={responseBody}")
        {
            Url = url;
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }

    public sealed class WebClientNetworkException : WebClientException
    {
        public string Url { get; }
        public string NetworkError { get; }

        public WebClientNetworkException(string url, string networkError, Exception innerException = null)
            : base($"Network request failed. Url={url}, Error={networkError}", innerException)
        {
            Url = url;
            NetworkError = networkError;
        }
    }
}
