using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public sealed class CardCollectionApplicationFacadeFactory : ICardCollectionApplicationFacadeFactory
    {
        private readonly ICardPackProvider _cardPackProvider;
        private readonly IEventCardsStorage _eventCardsStorage;
        private readonly ICardSelector _cardSelector;
        private readonly ICardPointsCalculator _cardPointsCalculator;

        public CardCollectionApplicationFacadeFactory(
            ICardPackProvider cardPackProvider,
            IEventCardsStorage eventCardsStorage,
            ICardSelector cardSelector,
            ICardPointsCalculator cardPointsCalculator)
        {
            _cardPackProvider = cardPackProvider;
            _eventCardsStorage = eventCardsStorage;
            _cardSelector = cardSelector;
            _cardPointsCalculator = cardPointsCalculator;
        }

        public async UniTask<ICardCollectionApplicationFacade> CreateInitializedAsync(
            CardCollectionStaticData staticData,
            string eventId,
            CancellationToken ct)
        {
            var cardDefinitionProvider = new DefaultCardDefinitionProvider(staticData.Cards);
            var cardPackService = new CardPackService(_cardPackProvider);
            var cardProgressService = new CardProgressService(_eventCardsStorage);
            var cardRandomizer = new PackBasedCardsRandomizer(_cardSelector, cardDefinitionProvider);
            var duplicatePointsCalculator = new DuplicateCardPointsCalculator(cardDefinitionProvider, _cardPointsCalculator);
            var openPackUseCase = new OpenPackUseCase(
                cardPackService,
                cardRandomizer,
                cardProgressService,
                duplicatePointsCalculator,
                cardDefinitionProvider);
            var unlockCardsUseCase = new UnlockCardsUseCase(cardProgressService, cardDefinitionProvider);
            var pointsAccountService = new PointsAccountService(cardProgressService);
            var progressQueryService = new CollectionProgressQueryService(cardProgressService);

            var facade = new CardCollectionApplicationFacade(
                eventId,
                cardDefinitionProvider,
                cardPackService,
                cardProgressService,
                openPackUseCase,
                unlockCardsUseCase,
                pointsAccountService,
                progressQueryService);
            
            await facade.InitializeAsync(ct);
            
            return facade;
        }
    }
}
