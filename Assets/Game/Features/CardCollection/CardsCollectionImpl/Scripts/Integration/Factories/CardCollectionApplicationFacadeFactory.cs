using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public sealed class CardCollectionApplicationFacadeFactory : ICardCollectionApplicationFacadeFactory
    {
        private readonly ICardSelector _cardSelector;
        private readonly IEventCardsStorage _eventCardsStorage;
        private readonly ICardPointsCalculator _cardPointsCalculator;

        public CardCollectionApplicationFacadeFactory(
            ICardSelector cardSelector,
            IEventCardsStorage eventCardsStorage,
            ICardPointsCalculator cardPointsCalculator)
        {
            _cardSelector = cardSelector;
            _eventCardsStorage = eventCardsStorage;
            _cardPointsCalculator = cardPointsCalculator;
        }

        public async UniTask<ICardCollectionApplicationFacade> CreateInitializedAsync(CardCollectionStaticData staticData, string eventId, CancellationToken ct)
        {
            var cardDefinitionProvider = new DefaultCardDefinitionProvider(staticData.Cards);
            var cardPackService = new CardPackService(staticData.Packs);
            var cardProgressService = new CardProgressService(_eventCardsStorage, cardDefinitionProvider, _cardPointsCalculator);
            var cardRandomizer = new PackBasedCardsRandomizer(_cardSelector, cardDefinitionProvider);
            var openPackUseCase = new OpenPackUseCase(
                cardPackService,
                cardRandomizer,
                cardProgressService,
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
