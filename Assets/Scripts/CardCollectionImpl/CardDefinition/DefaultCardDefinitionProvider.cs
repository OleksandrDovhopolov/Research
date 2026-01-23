using System.Collections.Generic;
using CardCollection.Core;

namespace core
{
    public class DefaultCardDefinitionProvider : ICardDefinitionProvider
    {
        private List<CardDefinition> _cache;

        public List<CardDefinition> GetCardDefinitions()
        {
            if (_cache != null) return _cache;
            
            var configs = CardCollectionConfigStorage.Instance.Data;
            var result = new List<CardDefinition>(configs.Count);

            foreach (var config in configs)
            {
                result.Add(new CardDefinition
                {
                    Id = config.Id,
                    CardName = config.CardName,
                    GroupType = config.GroupType,
                    Stars = config.Stars,
                    PremiumCard = config.PremiumCard,
                    Icon = config.Icon
                });
            }

            _cache = result;

            return _cache;
        }
    }
}