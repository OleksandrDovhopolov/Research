using System;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class CardCollectionEntryPoint : MonoBehaviour
    {
        private CardCollectionModule _cardCollectionModule;

        private readonly UniTaskCompletionSource _initializationSource = new();

        public bool IsInitialized => _initializationSource.Task.Status.IsCompleted();
        public UniTask WaitForInitializationAsync() => _initializationSource.Task;

        public ICardCollectionModule CardCollectionModule =>
            !IsInitialized ? throw new InvalidOperationException("Module not initialized.") : _cardCollectionModule;
        public ICardCollectionUpdater CardCollectionUpdater =>
            !IsInitialized ? throw new InvalidOperationException("Module not initialized.") : _cardCollectionModule;
        public ICardCollectionReader CardCollectionReader =>
            !IsInitialized ? throw new InvalidOperationException("Module not initialized.") : _cardCollectionModule;
        
        private void Awake()
        {
            IniCardCollection().Forget();
        }

        private async UniTask IniCardCollection()
        {
            try
            {
                ICardPackProvider packProvider = new JsonCardPackProvider();
                IEventCardsStorage cardsStorage = new JsonEventCardsStorage();
                ICardDefinitionProvider cardDefinitionProvider = new DefaultCardDefinitionProvider();
                ICardSelector cardSelector = new ProbabilityBasedCardSelector();

                const string testEventId = "test";
                var config = new CardCollectionModuleConfig(packProvider, cardsStorage, cardDefinitionProvider, cardSelector, testEventId);

                _cardCollectionModule = new CardCollectionModule(config);
                await _cardCollectionModule.InitializeAsync();

                _initializationSource.TrySetResult();
            }
            catch (Exception ex)
            {
                _initializationSource.TrySetException(ex);
            }
        }
    }
}