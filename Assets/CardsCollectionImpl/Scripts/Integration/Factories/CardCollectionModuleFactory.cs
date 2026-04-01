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
            var moduleConfig = new CardCollectionModuleConfig(
                _cardPackProvider,
                _eventCardsStorage,
                new DefaultCardDefinitionProvider(staticData.Cards),
                _cardSelector,
                _cardPointsCalculator,
                eventId);

            return new CardCollectionModule(moduleConfig);
        }
    }
}
