using Newtonsoft.Json;

namespace Infrastructure
{
    public static class WebClientErrorResponseParser
    {
        private const string InsufficientResourcesErrorCode = "insufficient_resources";

        public static bool IsInsufficientResources(long statusCode, string responseBody)
        {
            return statusCode == 409 &&
                   TryParse(responseBody, out var response) &&
                   string.Equals(response.ErrorCode, InsufficientResourcesErrorCode, System.StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryParse(string responseBody, out WebClientErrorResponse response)
        {
            response = null;
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return false;
            }

            try
            {
                response = JsonConvert.DeserializeObject<WebClientErrorResponse>(responseBody);
                return response != null;
            }
            catch
            {
                response = null;
                return false;
            }
        }
    }

    public sealed class WebClientErrorResponse
    {
        public bool Success { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
