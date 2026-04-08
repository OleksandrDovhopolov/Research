using System.Collections.Generic;

namespace CardCollection.Core
{
    public interface ICardDefinitionProvider
    {
        List<CardDefinition> GetCardDefinitions();
        IReadOnlyDictionary<string, CardDefinition> GetCardDefinitionsById();
    }
}