using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UISystem;
using UnityEngine;

namespace CardCollectionImpl
{
    public sealed class CardCollectionWindowOpener : ICardCollectionWindowOpener
    {
        private readonly UIManager _uiManager;
        private readonly ICardCollectionModule _module;
        private readonly ICardCollectionReader _reader;
        private readonly IReadOnlyList<CardConfig> _cards;
        private readonly IReadOnlyList<CardCollectionGroupConfig> _groups;
        private readonly ICardCollectionPointsAccount _pointsAccount;
        private readonly IExchangeOfferProvider _exchangeOfferProvider;
        private readonly IRewardDefinitionFactory _rewardDefinitionFactory;
        private readonly ICardCollectionCacheService _cardCollectionCacheService;
        private readonly CardCollectionRewardsConfigSO _collectionRewardsConfigSo;
        private readonly ICollectionProgressSnapshotService _collectionProgressSnapshotService;

        public CardCollectionWindowOpener(UIManager uiManager,
            ICardCollectionModule module,
            ICardCollectionReader reader,
            ICardCollectionPointsAccount pointsAccount,
            IReadOnlyList<CardConfig> cards,
            IReadOnlyList<CardCollectionGroupConfig> groups,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardDefinitionFactory rewardDefinitionFactory,
            ICardCollectionCacheService cardCollectionCacheService,
            ICollectionProgressSnapshotService collectionProgressSnapshotService,
            CardCollectionRewardsConfigSO rewardsConfig)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _pointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
            _collectionRewardsConfigSo = rewardsConfig ?? throw new ArgumentNullException(nameof(rewardsConfig));
            _cards = cards ?? throw new ArgumentNullException(nameof(cards));
            _groups = groups ?? throw new ArgumentNullException(nameof(groups));
            _exchangeOfferProvider = exchangeOfferProvider ?? throw new ArgumentNullException(nameof(exchangeOfferProvider));
            _rewardDefinitionFactory = rewardDefinitionFactory ?? throw new ArgumentNullException(nameof(rewardDefinitionFactory));
            _cardCollectionCacheService = cardCollectionCacheService ?? throw new ArgumentNullException(nameof(cardCollectionCacheService));
            _collectionProgressSnapshotService = collectionProgressSnapshotService ?? throw new ArgumentNullException(nameof(collectionProgressSnapshotService));
        }

        public void OpenNewCardWindow(string packId)
        {
            var pack = _module.GetPackById(packId);
            if (pack == null)
            {
                Debug.LogError($"Failed to find pack with id {packId}");
                return;
            }

            OpenNewCardWindow(pack);
        }

        public void OpenNewCardWindow(CardPack pack)
        {
            var args = new NewCardArgs(_module.EventId, pack, _module, _reader, _cardCollectionCacheService);
            _uiManager.Show<NewCardController>(args);
        }
        
        public async UniTask OpenCardGroupCompletedWindow(IEnumerable<string> groupTypes, CancellationToken ct)
        {
            var groupConfigs = new List<CardCollectionGroupConfig>();

            foreach (var groupType in groupTypes)
            {
                var groupConfig = _groups.FirstOrDefault(group => group.groupType == groupType);
                if (groupConfig == null)
                {
                    Debug.LogError($"Failed to find group {groupType}");
                    continue;
                }
                groupConfigs.Add(groupConfig);
            }
            
            var collectionData = await _reader.Load(ct);
            
            var args = new CardGroupCollectionArgs(_module.EventId, collectionData, groupConfigs, _collectionRewardsConfigSo);
            _uiManager.Show<CardGroupCompletedWindow>(args);
        }
        
        public async UniTask OpenCardCollectionWindow(CancellationToken ct)
        {
            var collectionData = await _reader.Load(ct);
            
            var newCardsData = CardCollectionNewCardsDto.Create(collectionData, _cards);
            var newCardIds = newCardsData.NewCardIds;

            if (newCardIds.Count > 0)
            {
                //TODO check do i need here await
                await _module.ResetNewFlagsAsync(newCardIds, ct);
            }

            _collectionProgressSnapshotService.TryGetSnapshot(out var snapshot);
            var args = new CardCollectionArgs(
                newCardsData,
                collectionData,
                _exchangeOfferProvider,
                _rewardDefinitionFactory, 
                _collectionRewardsConfigSo,
                _pointsAccount,
                snapshot,
                _module.EventId,
                _cards,
                _groups);
            _uiManager.Show<CardCollectionController>(args);

            _collectionProgressSnapshotService.SetSnapshot(collectionData);
        }
    }
}
