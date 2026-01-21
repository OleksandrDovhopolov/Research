using CardCollection.Core;
using cheatModule;
using Cysharp.Threading.Tasks;

namespace core
{
    public class DefaultModule : ICheatsModule
    {
        private readonly ICardCollectionUpdater _collectionUpdater;
        
        public DefaultModule(ICardCollectionUpdater collectionUpdater)
        {
            _collectionUpdater = collectionUpdater;
        }
        
        public void Initialize(ICheatsContainer cheatsContainer)
        {
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Save collection", () =>
            {
                _collectionUpdater.Save().Forget();
            }));
            
            cheatsContainer.AddItem<CheatButtonItem>(item => item.OnClick("Load collection", () =>
            {
                _collectionUpdater.Load().Forget();
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