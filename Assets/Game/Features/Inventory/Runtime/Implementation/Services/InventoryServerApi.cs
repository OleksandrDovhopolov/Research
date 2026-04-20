using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infrastructure;
using Inventory.API;

namespace Inventory.Implementation.Services
{
    public sealed class InventoryServerApi : IInventoryServerApi
    {
        private const string RemovePath = "inventory/remove";
        private const string RemoveBatchPath = "inventory/remove-batch";

        private readonly IWebClient _webClient;

        public InventoryServerApi(IWebClient webClient)
        {
            _webClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
        }

        public UniTask<InventoryOperationResponse> RemoveAsync(RemoveInventoryItemCommand command, CancellationToken cancellationToken = default)
        {
            return _webClient.PostAsync<RemoveInventoryItemCommand, InventoryOperationResponse>(RemovePath, command, cancellationToken);
        }

        public UniTask<InventoryOperationResponse> RemoveBatchAsync(RemoveInventoryBatchCommand command, CancellationToken cancellationToken = default)
        {
            return _webClient.PostAsync<RemoveInventoryBatchCommand, InventoryOperationResponse>(RemoveBatchPath, command, cancellationToken);
        }
    }
}
