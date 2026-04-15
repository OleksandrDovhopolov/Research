using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace CardCollectionImpl
{
    public sealed class CardCollectionStaticDataLoader : ICardCollectionStaticDataLoader
    {
        private readonly IEventConfigProvider _eventConfigProvider;

        public CardCollectionStaticDataLoader(IEventConfigProvider eventConfigProvider)
        {
            _eventConfigProvider = eventConfigProvider;
        }

        public async UniTask<CardCollectionStaticData> LoadAsync(CardCollectionEventModel model, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            
            _eventConfigProvider.ClearCache();
            await _eventConfigProvider.LoadAsync(model.EventConfigAddress, ct);

            return new CardCollectionStaticData
            {
                 EventConfig = _eventConfigProvider.Data,
            };
        }
    }
}
