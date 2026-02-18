using System.Collections.Generic;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;

namespace core
{
    public class ProbabilityBasedCardSelector : ICardSelector
    {
        private readonly PackStrategyRegistry _strategyRegistry;

        public ProbabilityBasedCardSelector(Dictionary<string, PackRule> packRules = null)
        {
            _strategyRegistry = new PackStrategyRegistry();

            if (packRules is not { Count: > 0 }) return;
            
            var packOpeningHistory = new PackOpeningHistory();

            foreach (var kvp in packRules)
            {
                _strategyRegistry.RegisterStrategy(
                    kvp.Key,
                    new SapphirePackStrategy(kvp.Value, packOpeningHistory));
            }
        }

        public async UniTask<List<string>> SelectCardsAsync(CardPack pack, List<CardDefinition> allCards, CardSelectionContext context, CancellationToken ct = default)
        {
            var packContext = context as PackSelectionContext
                              ?? new PackSelectionContext(context?.CardCollectionReader);

            var strategy = _strategyRegistry.GetStrategy(pack.PackId);
            
            return await strategy.SelectCardsAsync(pack, allCards, packContext, ct);
        }
    }
}
