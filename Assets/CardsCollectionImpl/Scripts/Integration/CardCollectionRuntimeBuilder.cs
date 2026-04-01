using System;
using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;

namespace CardCollectionImpl
{
    public sealed class CardCollectionRuntimeBuilder : ICardCollectionRuntimeBuilder
    {
        private readonly ICardCollectionCacheService _cardCollectionCacheService;
        private readonly ICardCollectionStaticDataLoader _staticDataLoader;
        private readonly ICardCollectionModuleFactory _moduleFactory;
        private readonly ICardCollectionSessionFactory _sessionFactory;
        
        public CardCollectionRuntimeBuilder(
            ICardCollectionCacheService  cardCollectionCacheService,
            ICardCollectionStaticDataLoader staticDataLoader,
            ICardCollectionModuleFactory moduleFactory,
            ICardCollectionSessionFactory sessionFactory)
        {
            _cardCollectionCacheService = cardCollectionCacheService;
            _staticDataLoader = staticDataLoader;
            _moduleFactory = moduleFactory;
            _sessionFactory = sessionFactory;
        }
        
        public async UniTask<CardCollectionSession> BuildAsync(CardCollectionEventModel model, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (model == null)
            {
                throw new ArgumentNullException($"CardCollectionEventModel is null {nameof(model)}");
            }
            
            var staticData = await _staticDataLoader.LoadAsync(model, ct);

            _cardCollectionCacheService.Initialize(staticData.Cards);
            CardCollectionModule module = null;
            
            try
            {
                module = _moduleFactory.Create(staticData, model.EventId);
                return await _sessionFactory.CreateAsync(model, staticData, module, ct);
            }
            catch
            {
                module?.Dispose();
                throw;
            }
        }
    }
    
    public class CardCollectionStaticData
    {
        public IReadOnlyList<CardPackConfig> Packs { get; set; }
        public IReadOnlyList<CardConfig> Cards { get; set; }
        public IReadOnlyList<CardCollectionGroupConfig> Groups { get; set; }
    }
}