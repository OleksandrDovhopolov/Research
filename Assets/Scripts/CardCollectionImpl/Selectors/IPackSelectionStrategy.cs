using System.Collections.Generic;
using CardCollection.Core;
using Cysharp.Threading.Tasks;

namespace core
{
    /// <summary>
    /// Strategy interface for pack-specific card selection logic.
    /// Each pack type can have its own strategy implementation.
    /// </summary>
    public interface IPackSelectionStrategy
    {
        /// <summary>
        /// Selects cards for a pack using pack-specific rules.
        /// </summary>
        /// <param name="pack">The pack to select cards for</param>
        /// <param name="allCards">List of all available cards</param>
        /// <param name="context">Context containing services and helpers</param>
        /// <returns>List of selected card IDs</returns>
        UniTask<List<string>> SelectCardsAsync(
            CardPack pack, 
            List<CardDefinition> allCards, 
            PackSelectionContext context);
    }
}
