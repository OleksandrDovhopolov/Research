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
        private readonly ICardCollectionApplicationFacadeFactory _facadeFactory;
        private readonly ICardCollectionSessionFactory _sessionFactory;
        
        public CardCollectionRuntimeBuilder(
            ICardCollectionCacheService  cardCollectionCacheService,
            ICardCollectionStaticDataLoader staticDataLoader,
            ICardCollectionApplicationFacadeFactory facadeFactory,
            ICardCollectionSessionFactory sessionFactory)
        {
            _cardCollectionCacheService = cardCollectionCacheService;
            _staticDataLoader = staticDataLoader;
            _facadeFactory = facadeFactory;
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
            ICardCollectionApplicationFacade facade = null;
            
            try
            {
                facade = await _facadeFactory.CreateInitializedAsync(staticData, model.EventId, ct);
                return _sessionFactory.Create(model, staticData, facade);
            }
            catch
            {
                facade?.Dispose();
                throw;
            }
        }
    }
    
    public class CardCollectionStaticData
    {
        public IReadOnlyList<CardPackConfig> Packs { get; set; }
        public IReadOnlyList<CardConfig> Cards { get; set; }
        public IReadOnlyList<CardCollectionGroupConfig> Groups { get; set; }
        public IReadOnlyList<RewardConfig> Rewards { get; set; }
    }
}