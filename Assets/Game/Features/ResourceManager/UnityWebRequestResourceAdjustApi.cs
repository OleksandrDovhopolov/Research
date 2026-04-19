using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using UnityEngine;
using UnityEngine.Networking;

namespace CoreResources
{
    public sealed class UnityWebRequestResourceAdjustApi : IResourceAdjustApi
    {
        private const string AdjustResourcePath = "resources/adjust";

        public async UniTask<AdjustResourceResponse> AdjustAsync(AdjustResourceCommand command, CancellationToken ct)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var payload = JsonUtility.ToJson(new AdjustResourceRequestBody
            {
                playerId = command.PlayerId,
                resourceId = command.ResourceId,
                delta = command.Delta,
                reason = command.Reason
            });

            using var request = new UnityWebRequest(ApiConfig.BaseUrl + AdjustResourcePath, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest().ToUniTask(cancellationToken: ct);

            if (request.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            {
                var body = request.downloadHandler?.text;
                throw new InvalidOperationException(
                    $"Resource adjust request failed. Status={(int)request.responseCode}, Error={request.error}, Body={body}");
            }

            var responseText = request.downloadHandler?.text;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new InvalidOperationException("Resource adjust response is empty.");
            }

            var parsed = JsonUtility.FromJson<AdjustResourceResponseBody>(responseText);
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
