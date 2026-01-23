using System.Collections.Generic;
using CardCollection.Core;
using Cysharp.Threading.Tasks;

namespace core
{
    /// <summary>
    /// Probability-based card selection strategy.
    /// Uses the Strategy pattern to delegate pack-specific selection logic to specialized strategies.
    /// </summary>
    public class ProbabilityBasedCardSelector : ICardSelector
    {
        private readonly PackStrategyRegistry _strategyRegistry;
        private readonly PackSelectionContext _context;
        
        public ProbabilityBasedCardSelector()
        {
            _strategyRegistry = new PackStrategyRegistry();
            var packOpeningHistory = new PackOpeningHistory();

            // Register pack-specific strategies
            _strategyRegistry.RegisterStrategy("Sapphire_Pack", new SapphirePackStrategy(packOpeningHistory));

            // Create context for strategies
            _context = new PackSelectionContext();
        }

        public void SetCardProgressService(ICardCollectionReader cardCollectionReader)
        {
            _context.CardCollectionReader = cardCollectionReader;
        }
        
        public async UniTask<List<string>> SelectCardsAsync(CardPack pack, List<CardDefinition> allCards)
        {
            // Get the appropriate strategy for this pack
            var strategy = _strategyRegistry.GetStrategy(pack.PackId);
            
            // Delegate to the strategy
            return await strategy.SelectCardsAsync(pack, allCards, _context);
        }
    }
}
