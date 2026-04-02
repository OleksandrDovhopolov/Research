using System;

namespace CardCollection.Core
{
    public sealed class CardCollectionModuleConfig
    {
        public ICardPackProvider PackProvider { get; }
        public ICardSelector CardSelector { get; }
        public IEventCardsStorage EventCardsStorage { get; }
        public ICardDefinitionProvider CardDefinitionProvider { get; }
        public ICardPointsCalculator CardPointsCalculator { get; }
        public IOpenPackUseCase OpenPackUseCase { get; }
        public IUnlockCardsUseCase UnlockCardsUseCase { get; }
        public IPointsAccountService PointsAccountService { get; }
        public ICollectionProgressQueryService ProgressQueryService { get; }
        public CardPackService SharedCardPackService { get; }
        public CardProgressService SharedCardProgressService { get; }
        public PackBasedCardsRandomizer SharedCardRandomizer { get; }
        public IDuplicateCardPointsCalculator SharedDuplicateCardPointsCalculator { get; }
        public string EventId { get; }

        public CardCollectionModuleConfig (
            ICardPackProvider packProvider,
            IEventCardsStorage eventCardsStorage,
            ICardDefinitionProvider cardDefinitionProvider,
            ICardSelector cardSelector,
            ICardPointsCalculator cardPointsCalculator,
            IOpenPackUseCase openPackUseCase,
            IUnlockCardsUseCase unlockCardsUseCase,
            IPointsAccountService pointsAccountService,
            ICollectionProgressQueryService progressQueryService,
            string eventId = "default",
            CardPackService sharedCardPackService = null,
            CardProgressService sharedCardProgressService = null,
            PackBasedCardsRandomizer sharedCardRandomizer = null,
            IDuplicateCardPointsCalculator sharedDuplicateCardPointsCalculator = null)
        {
            EventId = eventId;
            PackProvider = packProvider ?? throw new ArgumentNullException(nameof(packProvider));
            EventCardsStorage = eventCardsStorage ?? throw new ArgumentNullException(nameof(eventCardsStorage));
            CardDefinitionProvider = cardDefinitionProvider ?? throw new ArgumentNullException(nameof(cardDefinitionProvider));
            CardSelector = cardSelector ?? throw new ArgumentNullException(nameof(cardSelector));
            CardPointsCalculator = cardPointsCalculator ?? throw new ArgumentNullException(nameof(cardPointsCalculator));
            OpenPackUseCase = openPackUseCase ?? throw new ArgumentNullException(nameof(openPackUseCase));
            UnlockCardsUseCase = unlockCardsUseCase ?? throw new ArgumentNullException(nameof(unlockCardsUseCase));
            PointsAccountService = pointsAccountService ?? throw new ArgumentNullException(nameof(pointsAccountService));
            ProgressQueryService = progressQueryService ?? throw new ArgumentNullException(nameof(progressQueryService));
            SharedCardPackService = sharedCardPackService;
            SharedCardProgressService = sharedCardProgressService;
            SharedCardRandomizer = sharedCardRandomizer;
            SharedDuplicateCardPointsCalculator = sharedDuplicateCardPointsCalculator;
        }
    }
}