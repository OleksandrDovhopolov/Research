using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;

namespace CoreResources
{
    public sealed class UnityWebRequestResourceAdjustApi : IResourceAdjustApi
    {
        private const string AdjustResourcePath = "resources/adjust";
        private readonly IWebClient _webClient;

        public UnityWebRequestResourceAdjustApi(IWebClient webClient)
        {
            _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
        }

        public async UniTask<AdjustResourceResponse> AdjustAsync(AdjustResourceCommand command, CancellationToken ct)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var payload = new AdjustResourceRequestBody
            {
                playerId = command.PlayerId,
                resourceId = command.ResourceId,
                delta = command.Delta,
                reason = command.Reason
            };

            var parsed = await _webClient.PostAsync<AdjustResourceRequestBody, AdjustResourceResponseBody>(
                AdjustResourcePath,
                payload,
                ct);
            if (parsed == null)
            {
                throw new InvalidOperationException("Resource adjust response payload is invalid.");
            }

            return new AdjustResourceResponse
            {
                Success = parsed.success,
                ErrorCode = parsed.errorCode,
                ErrorMessage = parsed.errorMessage,
                Resources = parsed.resources == null
                    ? null
                    : new ResourceSnapshotDto
                    {
                        Gold = parsed.resources.gold,
                        Energy = parsed.resources.energy,
                        Gems = parsed.resources.gems
                    }
            };
        }

        [Serializable]
        private sealed class AdjustResourceRequestBody
        {
            public string playerId;
            public string resourceId;
            public int delta;
            public string reason;
        }

        [Serializable]
        private sealed class AdjustResourceResponseBody
        {
            public bool success;
            public string errorCode;
            public string errorMessage;
            public ResourceSnapshotResponseBody resources;
        }

        [Serializable]
        private sealed class ResourceSnapshotResponseBody
        {
            public int gold;
            public int energy;
            public int gems;
        }
    }
}
