using CardCollection.Core;
using cheatModule;
using Cysharp.Threading.Tasks;

namespace core
{
    public class DefaultModule : ICheatsModule
    {
        private const string CardCollectionPointsGroup = "CardCollectionPointsGroup";
        
        private readonly ICardCollectionUpdater _collectionUpdater;
        private readonly ICardCollectionReader _cardCollectionReader;
        private readonly ICardCollectionPointsAccount _cardCollectionPointsAccount;
        
        public DefaultModule(ICardCollectionUpdater collectionUpdater,ICardCollectionReader  cardCollectionReader, ICardCollectionPointsAccount  cardCollectionPointsAccount)
        {
            _collectionUpdater = collectionUpdater;
            _cardCollectionReader = cardCollectionReader;
            _cardCollectionPointsAccount = cardCollectionPointsAccount;
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
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Add points(int)", points =>
            {
                _cardCollectionPointsAccount.TryAddPointsAsync(points);
            }).WithGroup(CardCollectionPointsGroup));
            
            cheatsContainer.AddItem<CheatInputItem>(item => item.OnInputChange<int>("Remove points(int)", points =>
            {
                _cardCollectionPointsAccount.TrySpendPointsAsync(points);
            }).WithGroup(CardCollectionPointsGroup));
        }
    }
}
