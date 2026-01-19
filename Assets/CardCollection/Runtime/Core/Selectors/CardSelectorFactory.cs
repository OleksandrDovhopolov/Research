using CardCollection.Core;

namespace CardCollection.Core.Selectors
{
    /// <summary>
    /// Factory for creating card selectors based on pack configuration.
    /// Allows different packs to use different selection strategies.
    /// </summary>
    public static class CardSelectorFactory
    {
        /// <summary>
        /// Creates an appropriate card selector for the given pack.
        /// Can be extended to select different strategies based on pack properties.
        /// </summary>
        public static ICardSelector CreateSelector(CardPack pack, string serverUrl = null)
        {
            // For now, use random selection for all packs
            // In the future, you can add logic like:
            // - If pack requires server selection, return ServerCardSelector
            // - If pack has special rules, return RarityBasedCardSelector
            // - etc.
            
            // Example future logic:
            // if (pack.RequiresServerSelection)
            // {
            //     return new ServerCardSelector(serverUrl);
            // }
            
            return new RandomCardSelector();
        }

        /// <summary>
        /// Creates a server-based selector.
        /// </summary>
        public static ICardSelector CreateServerSelector(string serverUrl)
        {
            return new ServerCardSelector(serverUrl);
        }

        /// <summary>
        /// Creates a random selector.
        /// </summary>
        public static ICardSelector CreateRandomSelector()
        {
            return new RandomCardSelector();
        }
    }
}
