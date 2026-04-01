using CardCollection.Core;

namespace CardCollectionImpl
{
    public interface ICardCollectionModuleFactory
    {
        CardCollectionModule Create(CardCollectionStaticData staticData, string eventId);
    }
}
