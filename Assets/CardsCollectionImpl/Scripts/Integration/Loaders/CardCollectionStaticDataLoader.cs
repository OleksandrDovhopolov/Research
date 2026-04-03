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
        private readonly ICardsConfigProvider _cardsConfigProvider;
        private readonly IEventRewardsConfigProvider _eventRewardsConfigProvider;
        
        private readonly IEventConfigProvider _eventConfigProvider;

        public CardCollectionStaticDataLoader(
            IEventConfigProvider eventConfigProvider,
            ICardPackProvider cardPackProvider,
            ICardsConfigProvider cardsConfigProvider,
            IEventRewardsConfigProvider eventRewardsConfigProvider)
        {
            _eventConfigProvider = eventConfigProvider;
            _cardPackProvider = cardPackProvider;
            _cardsConfigProvider = cardsConfigProvider;
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
            
            _cardsConfigProvider.ClearCache();
            _cardPackProvider.ClearCache();
            _eventRewardsConfigProvider.ClearCache();

            await UniTask.WhenAll(
                _cardPackProvider.LoadAsync(model.CardPacksFileName, ct),
                _cardsConfigProvider.LoadAsync(model.CardCollectionFileName, ct),
                _eventRewardsConfigProvider.LoadAsync(model.RewardsConfigAddress, ct),
                
                _eventConfigProvider.LoadAsync(model.EventConfigAddress, ct)
                );

            return new CardCollectionStaticData
            {
                EventConfig = _eventConfigProvider.Data,
                Packs = _cardPackProvider.Data,
                Cards = _cardsConfigProvider.Data,
                Rewards = _eventRewardsConfigProvider.Data
            };
        }
    }
}
