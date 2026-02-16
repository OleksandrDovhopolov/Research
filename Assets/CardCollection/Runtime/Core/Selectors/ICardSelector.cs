using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace CardCollection.Core
{
    /// <summary>
    /// Interface for selecting cards from a pack.
    /// Different implementations can provide different selection strategies:
    /// - Random selection (local)
    /// - Server-based selection
    /// - Rarity-based selection
    /// - etc.
    /// </summary>
    public interface ICardSelector
    {
        /// <summary>
        /// Selects card IDs for the given pack.
        /// </summary>
        /// <param name="pack">The pack to select cards for</param>
        /// <param name="allCards">List of all available cards to select from</param>
        /// <param name="context">Context providing collection state for strategies that need it</param>
        /// <returns>List of selected card IDs</returns>
        UniTask<List<string>> SelectCardsAsync(CardPack pack, List<CardDefinition> allCards, CardSelectionContext context);
    }
}
