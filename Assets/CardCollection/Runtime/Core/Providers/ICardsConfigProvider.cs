using System.Collections.Generic;

namespace CardCollection.Core
{
    public interface ICardsConfigProvider: IStaticDataProvider<List<CardConfig>>
    {
    }
}