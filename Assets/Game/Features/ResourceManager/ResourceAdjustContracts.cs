using System;

namespace CoreResources
{
    [Serializable]
    public sealed class AdjustResourceCommand
    {
        public string PlayerId { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public int Delta { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    [Serializable]
    public sealed class AdjustResourceResponse
    {
        public bool Success { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public ResourceSnapshotDto Resources { get; set; }
    }

    [Serializable]
    public sealed class ResourceSnapshotDto
    {
        public int Gold { get; set; }
        public int Energy { get; set; }
        public int Gems { get; set; }
    }
}
