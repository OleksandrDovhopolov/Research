using System;

namespace CardCollection.Core
{
    /// <summary>
    /// Configuration object used to wire external implementations into the card collection module.
    /// A host project provides concrete providers/selectors/storage here.
    /// </summary>
    public sealed class CardCollectionModuleConfig
    {
        public ICardPackProvider PackProvider { get; }
        public ICardSelector CardSelector { get; }
        public IEventCardsStorage EventCardsStorage { get; }
        public ICardDefinitionProvider CardDefinitionProvider { get; }
        public string DefaultEventId { get; }

        public CardCollectionModuleConfig (
            ICardPackProvider packProvider,
            IEventCardsStorage eventCardsStorage,
            ICardDefinitionProvider cardDefinitionProvider,
            ICardSelector cardSelector,
            string defaultEventId = "default")
        {
            PackProvider = packProvider ?? throw new ArgumentNullException(nameof(packProvider));
            EventCardsStorage = eventCardsStorage ?? throw new ArgumentNullException(nameof(eventCardsStorage));
            CardDefinitionProvider = cardDefinitionProvider ?? throw new ArgumentNullException(nameof(cardDefinitionProvider));
            CardSelector = cardSelector ?? throw new ArgumentNullException(nameof(cardSelector));
            DefaultEventId = defaultEventId;
        }
    }
}