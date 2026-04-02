using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    /// <summary>
    /// Internal context that constructs and owns all core services.
    /// Serves as a lifecycle owner and dependency holder for the module.
    /// </summary>
    internal sealed class CardCollectionContext : IDisposable
    {
        private readonly CardCollectionModuleConfig _config;

        internal CardPackService CardPackService { get; }
        internal PackBasedCardsRandomizer CardRandomizer { get; }
        internal CardProgressService CardProgressService { get; }
        internal IDuplicateCardPointsCalculator DuplicateCardPointsCalculator { get; }
        internal ICardDefinitionProvider CardDefinitionProvider => _config.CardDefinitionProvider;
        internal IOpenPackUseCase OpenPackUseCase => _config.OpenPackUseCase;
        internal IUnlockCardsUseCase UnlockCardsUseCase => _config.UnlockCardsUseCase;
        internal IPointsAccountService PointsAccountService => _config.PointsAccountService;
        internal ICollectionProgressQueryService ProgressQueryService => _config.ProgressQueryService;

        public string EventId { get; }

        public CardCollectionContext(CardCollectionModuleConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.PackProvider == null) throw new ArgumentNullException(nameof(config.PackProvider));
            if (config.CardSelector == null) throw new ArgumentNullException(nameof(config.CardSelector));
            if (config.EventCardsStorage == null) throw new ArgumentNullException(nameof(config.EventCardsStorage));

            _config = config;

            EventId = _config.EventId;
            CardPackService = _config.SharedCardPackService ?? new CardPackService(_config.PackProvider);
            CardProgressService = _config.SharedCardProgressService ?? new CardProgressService(_config.EventCardsStorage);
            CardRandomizer = _config.SharedCardRandomizer ?? new PackBasedCardsRandomizer(_config.CardSelector, _config.CardDefinitionProvider);
            DuplicateCardPointsCalculator = _config.SharedDuplicateCardPointsCalculator ??
                                            new DuplicateCardPointsCalculator(_config.CardDefinitionProvider, _config.CardPointsCalculator);
        }

        public async UniTask InitializeAsync(CancellationToken ct = default)
        {
            await CardPackService.InitializeAsync(ct);
            await CardProgressService.InitializeAsync(ct);
            await CardProgressService.LoadAsync(EventId, ct);
        }

        public void Dispose()
        {
            //TODO add clearCache to ICardGroupsConfigProvider
            //TODO add clearCache to ICardsConfigProvider
            _config.PackProvider.ClearCache();
            CardPackService.Dispose();
            CardProgressService.Dispose();
        }
    }
}
