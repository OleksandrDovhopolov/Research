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
        private readonly IReadOnlyList<CardConfig> _cards;
        private readonly ICardCollectionModule _collectionModule;
        private readonly ICardCollectionRewardHandler _rewardHandler;
        private readonly IExchangeOfferProvider _exchangeOfferProvider;
        private readonly IReadOnlyList<CardCollectionGroupConfig> _groups;
        private readonly ICardCollectionPointsAccount _collectionPointsAccount;
        private readonly ICollectionProgressSnapshotBuilder _collectionProgressSnapshotBuilder;

        public CardCollectionWindowOpener(UIManager uiManager,
            ICardCollectionModule module,
            ICardCollectionPointsAccount pointsAccount,
            IReadOnlyList<CardConfig> cards,
            IReadOnlyList<CardCollectionGroupConfig> groups,
            IExchangeOfferProvider exchangeOfferProvider,
            ICollectionProgressSnapshotBuilder collectionProgressSnapshotBuilder,
            ICardCollectionRewardHandler rewardHandler)
        {
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _collectionModule = module ?? throw new ArgumentNullException(nameof(module));
            _collectionPointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
            _rewardHandler = rewardHandler ?? throw new ArgumentNullException(nameof(rewardHandler));
            _cards = cards ?? throw new ArgumentNullException(nameof(cards));
            _groups = groups ?? throw new ArgumentNullException(nameof(groups));
            _exchangeOfferProvider = exchangeOfferProvider ?? throw new ArgumentNullException(nameof(exchangeOfferProvider));
            _collectionProgressSnapshotBuilder = collectionProgressSnapshotBuilder ?? throw new ArgumentNullException(nameof(collectionProgressSnapshotBuilder));
        }

        public void OpenNewCardWindow(string packId)
        {
            var pack = _collectionModule.GetPackById(packId);
            if (pack == null)
            {
                Debug.LogError($"Failed to find pack with id {packId}");
                return;
            }

            var args = new NewCardArgs(_collectionModule.EventId, packId, _collectionModule, _collectionPointsAccount);
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
            
            var collectionData = await _collectionModule.Load(ct);
            
            var args = new CardGroupCollectionArgs(_collectionModule.EventId, collectionData, groupConfigs, _rewardHandler);
            _uiManager.Show<CardGroupCompletedWindow>(args);
        }
        
        public async UniTask OpenCardCollectionWindow(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var beforeResetData = await _collectionModule.Load(ct);
            var snapshotBeforeReset = _collectionProgressSnapshotBuilder.Build(beforeResetData);
            
            var newCardsData = CardCollectionNewCardsDto.Create(beforeResetData, _cards);
            var newCardIds = newCardsData.NewCardIds;
            if (newCardIds.Count > 0)
            {
                await _collectionModule.ResetNewFlagsAsync(newCardIds, ct);
            }
            var afterResetData = await _collectionModule.Load(ct);
            
            var args = new CardCollectionArgs(
                newCardsData,
                afterResetData,
                _exchangeOfferProvider,
                _rewardHandler,
                _collectionPointsAccount,
                snapshotBeforeReset,
                _collectionModule.EventId,
                _cards,
                _groups);
            _uiManager.Show<CardCollectionController>(args);
        }
    }
}
