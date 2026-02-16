namespace CardCollection.Core
{
    /// <summary>
    /// Context passed to <see cref="ICardSelector.SelectCardsAsync"/> so that
    /// card-selection strategies can query collection state (e.g. missing cards)
    /// without smuggling dependencies through back-channels.
    /// </summary>
    public class CardSelectionContext
    {
        /// <summary>
        /// Provides read-only access to the card collection progress.
        /// May be <c>null</c> when the selector does not need collection state.
        /// </summary>
        public ICardCollectionReader CardCollectionReader { get; }

        public CardSelectionContext(ICardCollectionReader cardCollectionReader = null)
        {
            CardCollectionReader = cardCollectionReader;
        }
    }
}
