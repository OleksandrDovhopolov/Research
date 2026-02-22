using System;
using System.Collections.Generic;
using CardCollection.Core;

namespace core
{
    public class DefaultCardDefinitionProvider : ICardDefinitionProvider
    {
        private List<CardDefinition> _cache;
        private Dictionary<string, CardDefinition> _cacheById;

        public List<CardDefinition> GetCardDefinitions()
        {
            EnsureCacheBuilt();
            return _cache;
        }

        public IReadOnlyDictionary<string, CardDefinition> GetCardDefinitionsById()
        {
            EnsureCacheBuilt();
            return _cacheById;
        }

        private void EnsureCacheBuilt()
        {
            if (_cache != null && _cacheById != null) return;

            var configs = CardCollectionConfigStorage.Instance.Data;
            var result = new List<CardDefinition>(configs.Count);
            var byId = new Dictionary<string, CardDefinition>(configs.Count);

            foreach (var config in configs)
            {
                var definition = new CardDefinition
                {
                    Id = config.Id,
                    CardName = config.CardName,
                    GroupType = config.GroupType,
                    Stars = config.Stars,
                    PremiumCard = config.PremiumCard,
                    Icon = config.Icon
                };

                result.Add(definition);

                if (!string.IsNullOrEmpty(definition.Id))
                {
                    // Last write wins to avoid runtime exceptions on malformed config duplicates.
                    byId[definition.Id] = definition;
                }
            }

            _cache = result;
            _cacheById = byId;
        }
    }
}