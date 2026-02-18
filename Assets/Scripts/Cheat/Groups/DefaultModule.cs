using CardCollection.Core;
using cheatModule;
using Cysharp.Threading.Tasks;

namespace core
{
    public class DefaultModule : ICheatsModule
    {
        private readonly ICardCollectionUpdater _collectionUpdater;
        private readonly ICardCollectionReader _cardCollectionReader;
        
        public DefaultModule(ICardCollectionUpdater collectionUpdater,ICardCollectionReader  cardCollectionReader)
        {
            _collectionUpdater = collectionUpdater;
            _cardCollectionReader = cardCollectionReader;
        }
        
        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Save collection", () =>
            {
                _collectionUpdater.Save().Forget();
            }));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Load collection", () =>
            {
                _cardCollectionReader.Load().Forget();
            }));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Clear collection", () =>
            {
                _collectionUpdater.Clear().Forget();
            }));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<string>("Open card ID(str)", cardId =>
            {
                _collectionUpdater.UnlockCard(cardId);
            }));
        }
    }
}
