using CardCollection.Core;
using EventOrchestration.Models;

namespace CardCollectionImpl
{
    public interface ICardCollectionSessionFactory
    {
        CardCollectionSession Create(CardCollectionEventModel model, CardCollectionStaticData staticData, ICardCollectionApplicationFacade facade);
    }
}
