using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using Rewards;
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
        private readonly IRewardSpecProvider _rewardSpecProvider;
        private readonly ICardCollectionCacheService _cardCollectionCacheService;
        private readonly ICardCollectionRewardHandler _rewardHandler;
        private readonly ICollectionProgressSnapshotService _collectionProgressSnapshotService;

        public CardCollectionWindowOpener(UIManager uiManager,
            ICardCollectionModule module,
            ICardCollectionReader reader,
            ICardCollectionPointsAccount pointsAccount,
            IReadOnlyList<CardConfig> cards,
            IReadOnlyList<CardCollectionGroupConfig> groups,
            IExchangeOfferProvider exchangeOfferProvider,
            IRewardSpecProvider rewardSpecProvider,
            ICardCollectionCacheService cardCollectionCacheService,
            ICollectionProgressSnapshotService collectionProgressSnapshotService,
            ICardCollectionRewardHandler rewardHandler)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _module = module ?? throw new ArgumentNullException(nameof(module));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _pointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
            _rewardHandler = rewardHandler ?? throw new ArgumentNullException(nameof(rewardHandler));
            _cards = cards ?? throw new ArgumentNullException(nameof(cards));
            _groups = groups ?? throw new ArgumentNullException(nameof(groups));
            _exchangeOfferProvider = exchangeOfferProvider ?? throw new ArgumentNullException(nameof(exchangeOfferProvider));
            _rewardSpecProvider = rewardSpecProvider ?? throw new ArgumentNullException(nameof(rewardSpecProvider));
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

            var args = new NewCardArgs(_module.EventId, packId, _module, _reader, _cardCollectionCacheService);
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
            
            var args = new CardGroupCollectionArgs(_module.EventId, collectionData, groupConfigs, _rewardHandler);
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
                _rewardSpecProvider, 
                _rewardHandler,
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
