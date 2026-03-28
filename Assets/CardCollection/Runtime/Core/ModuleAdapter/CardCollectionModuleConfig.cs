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
        public string EventId { get; }

        public CardCollectionModuleConfig (
            ICardPackProvider packProvider,
            IEventCardsStorage eventCardsStorage,
            ICardDefinitionProvider cardDefinitionProvider,
            ICardSelector cardSelector,
            ICardPointsCalculator cardPointsCalculator,
            string eventId = "default")
        {
            EventId = eventId;
            PackProvider = packProvider ?? throw new ArgumentNullException(nameof(packProvider));
            EventCardsStorage = eventCardsStorage ?? throw new ArgumentNullException(nameof(eventCardsStorage));
            CardDefinitionProvider = cardDefinitionProvider ?? throw new ArgumentNullException(nameof(cardDefinitionProvider));
            CardSelector = cardSelector ?? throw new ArgumentNullException(nameof(cardSelector));
            CardPointsCalculator = cardPointsCalculator ?? throw new ArgumentNullException(nameof(cardPointsCalculator));
        }
    }
}