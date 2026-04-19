using System;
using System.Collections.Generic;

namespace Infrastructure
{
    public sealed class WebClientOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public IReadOnlyDictionary<string, string> DefaultHeaders { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
