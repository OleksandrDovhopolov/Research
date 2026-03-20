using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Inventory.API;
using Resources.Core;

namespace CardCollectionImpl
{
    public class DebugRewardCreator
    {
        //TODO remove this from field
        private const string InventoryOwnerId = "player_1";
        
        private ICardCollectionRewardHandler _rewardHandler;
        
        private readonly UniTaskCompletionSource _rewardHandlerInitializationSource = new();

        private readonly ResourceManager _resourceManager;
        private readonly IInventoryService _inventoryService;
        private readonly IRewardDefinitionFactory _rewardDefinitionFactory;
        
        public ICardCollectionRewardHandler RewardHandler => _rewardHandler;
        
        public DebugRewardCreator(
            ResourceManager resourceManager, 
            IInventoryService  inventoryService,
            IRewardDefinitionFactory  rewardDefinitionFactory)
        {
            _resourceManager =  resourceManager;
            _inventoryService = inventoryService;
            _rewardDefinitionFactory = rewardDefinitionFactory;
        }
        
        public async UniTask InitializeRewardHandlerAsync(CancellationToken ct = default)
        {
            try
            {
                var rewardGrantService = new GameRewardGrantService(_resourceManager, _inventoryService, InventoryOwnerId);
                
                _rewardHandler = new CardCollectionRewardHandler(rewardGrantService, _rewardDefinitionFactory);
                await _rewardHandler.InitializeAsync(ct);
                _rewardHandlerInitializationSource.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                _rewardHandlerInitializationSource.TrySetCanceled(ct);
                throw;
            }
            catch (Exception ex)
            {
                _rewardHandlerInitializationSource.TrySetException(ex);
                throw;
            }
        }
    }
}