using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using Infrastructure;
using Inventory.API;
using Rewards;
using UIShared;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardCollectionSessionFactory : ICardCollectionSessionFactory
    {
        private readonly UIManager _uiManager;
        private readonly IHUDService _hudService;
        private readonly IRewardSpecProvider _rewardSpecProvider;
        private readonly ExchangePacksConfig _exchangePacksConfig;
        private readonly IRewardGrantService _rewardGrantService;
        private readonly IItemCategoryRegistry _itemCategoryRegistry;
        private readonly IInventoryUseHandlerStorage _inventoryUseHandlerStorage;
        private readonly ICardCollectionCacheService _cardCollectionCacheService;

        public CardCollectionSessionFactory(
            UIManager uiManager,
            IHUDService hudService,
            IRewardSpecProvider rewardSpecProvider,
            ExchangePacksConfig exchangePacksConfig,
            IRewardGrantService rewardGrantService,
            IItemCategoryRegistry itemCategoryRegistry,
            IInventoryUseHandlerStorage inventoryUseHandlerStorage,
            ICardCollectionCacheService cardCollectionCacheService)
        {
            _uiManager = uiManager;
            _hudService = hudService;
            _rewardSpecProvider = rewardSpecProvider;
            _exchangePacksConfig = exchangePacksConfig;
            _rewardGrantService = rewardGrantService;
            _itemCategoryRegistry = itemCategoryRegistry;
            _inventoryUseHandlerStorage = inventoryUseHandlerStorage;
            _cardCollectionCacheService = cardCollectionCacheService;
        }

        public async UniTask<CardCollectionSession> CreateAsync(
            CardCollectionEventModel model,
            CardCollectionStaticData staticData,
            ICardCollectionApplicationFacade facade,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            /*var rewardsConfig = await ProdAddressablesWrapper
                .LoadAsync<CardCollectionRewardsConfigSO>(model.RewardsConfigAddress, ct)
                .AsUniTask();
            ct.ThrowIfCancellationRequested();

            if (rewardsConfig == null)
            {
                Debug.LogError($"RewardsConfig not found with ID {model.RewardsConfigAddress}!");
            }*/

            var rewardHandler = new CardCollectionRewardHandler(staticData, _rewardSpecProvider, _rewardGrantService);
            var snapshotBuilder = new CollectionProgressSnapshotBuilder(_cardCollectionCacheService, staticData.Groups);
            var exchangeOfferProvider = new ExchangeOfferProvider(_exchangePacksConfig, rewardHandler);

            var windowOpener = new CardCollectionWindowOpener(
                _uiManager,
                facade,
                facade,
                staticData.Cards,
                staticData.Groups,
                exchangeOfferProvider,
                snapshotBuilder,
                rewardHandler);

            var hudPresenter = new CardCollectionHudPresenter(_hudService, windowOpener);
            var inventoryIntegration = new CardCollectionInventoryIntegration(_itemCategoryRegistry, _inventoryUseHandlerStorage, windowOpener);
            var context = new CardCollectionSessionContext(facade, facade, windowOpener);

            return new CardCollectionSession(
                _uiManager,
                context,
                facade,
                hudPresenter,
                rewardHandler,
                inventoryIntegration);
        }
    }
}
