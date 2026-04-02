using CardCollection.Core;

namespace CardCollectionImpl
{
    public sealed class CardCollectionModuleFactory : ICardCollectionModuleFactory
    {
        private readonly ICardPackProvider _cardPackProvider;
        private readonly IEventCardsStorage _eventCardsStorage;
        private readonly ICardSelector _cardSelector;
        private readonly ICardPointsCalculator _cardPointsCalculator;

        public CardCollectionModuleFactory(
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

        public CardCollectionModule Create(CardCollectionStaticData staticData, string eventId)
        {
            var cardDefinitionProvider = new DefaultCardDefinitionProvider(staticData.Cards);
            var cardPackService = new CardPackService(_cardPackProvider);
            var cardProgressService = new CardProgressService(_eventCardsStorage);
            var cardRandomizer = new PackBasedCardsRandomizer(_cardSelector, cardDefinitionProvider);
            var duplicatePointsCalculator = new DuplicateCardPointsCalculator(cardDefinitionProvider, _cardPointsCalculator);

            var moduleConfig = new CardCollectionModuleConfig(
                _cardPackProvider,
                _eventCardsStorage,
                cardDefinitionProvider,
                _cardSelector,
                _cardPointsCalculator,
                new OpenPackUseCase(cardPackService, cardRandomizer, cardProgressService, duplicatePointsCalculator),
                new UnlockCardsUseCase(cardProgressService),
                new PointsAccountService(cardProgressService),
                new CollectionProgressQueryService(cardProgressService),
                eventId,
                cardPackService,
                cardProgressService,
                cardRandomizer,
                duplicatePointsCalculator);

            return new CardCollectionModule(moduleConfig);
        }
    }
}
