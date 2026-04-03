using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace CardCollectionImpl
{
    public sealed class CardCollectionStaticDataLoader : ICardCollectionStaticDataLoader
    {
        private readonly ICardPackProvider _cardPackProvider;
        private readonly IEventRewardsConfigProvider _eventRewardsConfigProvider;
        
        private readonly IEventConfigProvider _eventConfigProvider;

        public CardCollectionStaticDataLoader(
            IEventConfigProvider eventConfigProvider,
            ICardPackProvider cardPackProvider,
            IEventRewardsConfigProvider eventRewardsConfigProvider)
        {
            _eventConfigProvider = eventConfigProvider;
            _cardPackProvider = cardPackProvider;
            _eventRewardsConfigProvider = eventRewardsConfigProvider;
        }

        public async UniTask<CardCollectionStaticData> LoadAsync(CardCollectionEventModel model, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            
            _eventConfigProvider.ClearCache();
            
            _cardPackProvider.ClearCache();
            _eventRewardsConfigProvider.ClearCache();

            await UniTask.WhenAll(
                _cardPackProvider.LoadAsync(model.CardPacksFileName, ct),
                _eventRewardsConfigProvider.LoadAsync(model.RewardsConfigAddress, ct),
                
                _eventConfigProvider.LoadAsync(model.EventConfigAddress, ct)
                );

            return new CardCollectionStaticData
            {
                EventConfig = _eventConfigProvider.Data,
                Packs = _cardPackProvider.Data,
                Rewards = _eventRewardsConfigProvider.Data
            };
        }
    }
}
