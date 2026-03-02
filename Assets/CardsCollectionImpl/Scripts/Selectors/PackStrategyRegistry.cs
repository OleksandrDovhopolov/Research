using System.Collections.Generic;

namespace CardCollectionImpl
{
    /// <summary>
    /// Registry that maps pack IDs to their selection strategies.
    /// </summary>
    public class PackStrategyRegistry
    {
        private readonly Dictionary<string, IPackSelectionStrategy> _strategies = new();

        public PackStrategyRegistry()
        {
            // Register default strategy
            RegisterDefaultStrategy();
        }

        /// <summary>
        /// Registers a strategy for a specific pack ID.
        /// </summary>
        public void RegisterStrategy(string packId, IPackSelectionStrategy strategy)
        {
            if (string.IsNullOrEmpty(packId))
                return;

            _strategies[packId] = strategy;
        }

        /// <summary>
        /// Gets the strategy for a pack ID, or returns the default strategy if not found.
        /// </summary>
        public IPackSelectionStrategy GetStrategy(string packId)
        {
            if (_strategies.TryGetValue(packId, out var strategy))
            {
                return strategy;
            }

            return _strategies["default"];
        }

        private void RegisterDefaultStrategy()
        {
            _strategies["default"] = new DefaultPackStrategy();
        }
    }
}
