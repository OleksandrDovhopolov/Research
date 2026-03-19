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
        private ICardCollectionRewardHandler _rewardHandler;
        
        private readonly UniTaskCompletionSource _rewardHandlerInitializationSource = new();

        private string _inventoryOwnerId;
        private ResourceManager _resourceManager;
        private IInventoryService _inventoryService;
        private ICardCollectionCompositionRoot _compositionRoot;
        private IRewardDefinitionFactory _rewardDefinitionFactory;
        
        public ICardCollectionRewardHandler RewardHandler => _rewardHandler;
        
        public DebugRewardCreator(
            ResourceManager resourceManager, 
            IInventoryService  inventoryService,
            ICardCollectionCompositionRoot  compositionRoot,
            IRewardDefinitionFactory  rewardDefinitionFactory,
            string inventoryOwnerId)
        {
            _resourceManager =  resourceManager;
            _inventoryOwnerId =  inventoryOwnerId;
            _compositionRoot = compositionRoot;
            _inventoryService = inventoryService;
            _rewardDefinitionFactory = rewardDefinitionFactory;
        }
        
        public async UniTask InitializeRewardHandlerAsync(CancellationToken ct = default)
        {
            try
            {
                var rewardGrantService = new GameRewardGrantService(_resourceManager, _inventoryService, _inventoryOwnerId);
                
                _rewardHandler = _compositionRoot.CreateRewardHandler(rewardGrantService, _rewardDefinitionFactory);
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