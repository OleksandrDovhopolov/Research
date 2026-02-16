using System;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class CardCollectionEntryPoint : MonoBehaviour
    {
        private CardCollectionModule _cardCollectionModule;

        private bool _isInitialized;

        public ICardCollectionModule CardCollectionModule =>
            !_isInitialized ? throw new InvalidOperationException("Module not initialized.") : _cardCollectionModule;
        public ICardCollectionUpdater CardCollectionUpdater =>
            !_isInitialized ? throw new InvalidOperationException("Module not initialized.") : _cardCollectionModule;
        public ICardCollectionReader CardCollectionReader =>
            !_isInitialized ? throw new InvalidOperationException("Module not initialized.") : _cardCollectionModule;

        private void Awake()
        {
            IniCardCollection().Forget();
        }

        private async UniTask IniCardCollection()
        {
            ICardPackProvider packProvider = new JsonCardPackProvider();
            IEventCardsStorage cardsStorage = new JsonEventCardsStorage();
            ICardDefinitionProvider cardDefinitionProvider = new DefaultCardDefinitionProvider();
            ICardSelector cardSelector = new ProbabilityBasedCardSelector();

            const string testEventId = "test";
            var config = new CardCollectionModuleConfig(packProvider, cardsStorage, cardDefinitionProvider, cardSelector, testEventId);

            _cardCollectionModule = new CardCollectionModule(config);
            await _cardCollectionModule.InitializeAsync();
            
            _isInitialized = true;
        }
    }
}