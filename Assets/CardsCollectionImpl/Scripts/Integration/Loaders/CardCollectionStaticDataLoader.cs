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
        
        private readonly IEventConfigProvider _eventConfigProvider;

        public CardCollectionStaticDataLoader(
            IEventConfigProvider eventConfigProvider,
            ICardPackProvider cardPackProvider)
        {
            _eventConfigProvider = eventConfigProvider;
            _cardPackProvider = cardPackProvider;
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

            await UniTask.WhenAll(
                _cardPackProvider.LoadAsync(model.CardPacksFileName, ct),
                
                _eventConfigProvider.LoadAsync(model.EventConfigAddress, ct)
                );

            return new CardCollectionStaticData
            {
                EventConfig = _eventConfigProvider.Data,
                Packs = _cardPackProvider.Data,
            };
        }
    }
}
