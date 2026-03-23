using System.Collections.Generic;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public class DefaultCardDefinitionProvider : ICardDefinitionProvider
    {
        private List<CardDefinition> _cache;
        private Dictionary<string, CardDefinition> _cacheById;

        private readonly IReadOnlyCollection<CardConfig> _data;
        
        public DefaultCardDefinitionProvider(IReadOnlyList<CardConfig> data)
        {
            _data = data;
        }
        
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

            var result = new List<CardDefinition>(_data.Count);
            var byId = new Dictionary<string, CardDefinition>(_data.Count);

            foreach (var config in _data)
            {
                var definition = new CardDefinition
                {
                    Id = config.id,
                    CardName = config.cardName,
                    GroupType = config.groupType,
                    Stars = config.stars,
                    PremiumCard = config.premiumCard,
                    Icon = config.icon
                };

                result.Add(definition);

                if (!string.IsNullOrEmpty(definition.Id))
                {
                    byId[definition.Id] = definition;
                }
            }

            _cache = result;
            _cacheById = byId;
        }
    }
}