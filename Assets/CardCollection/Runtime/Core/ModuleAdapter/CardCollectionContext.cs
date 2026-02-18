using System;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    /// <summary>
    /// Internal context that constructs and owns all core services.
    /// </summary>
    internal sealed class CardCollectionContext : IDisposable
    {
        private readonly CardCollectionModuleConfig _config;
        
        public CardPackService CardPackService { get; }
        public PackBasedCardsRandomizer CardRandomizer { get; }
        public CardProgressService CardProgressService { get; }
        public ICardDefinitionProvider CardDefinitionProvider => _config.CardDefinitionProvider;
        public string DefaultEventId { get; }

        public CardCollectionContext(CardCollectionModuleConfig  config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.PackProvider == null) throw new ArgumentNullException(nameof(config.PackProvider));
            if (config.CardSelector == null) throw new ArgumentNullException(nameof(config.CardSelector));
            if (config.EventCardsStorage == null) throw new ArgumentNullException(nameof(config.EventCardsStorage));

            _config =  config;
            
            DefaultEventId = _config.DefaultEventId;
            CardPackService = new CardPackService(_config.PackProvider);
            CardProgressService = new CardProgressService(_config.EventCardsStorage);
            CardRandomizer = new PackBasedCardsRandomizer(_config.CardSelector, _config.CardDefinitionProvider);
        }

        public async UniTask InitializeAsync()
        {
            await CardPackService.InitializeAsync();
            await CardProgressService.InitializeAsync();
        }

        public void Dispose()
        {
            CardPackService.Dispose();
            CardProgressService.Dispose();
        }
    }
}