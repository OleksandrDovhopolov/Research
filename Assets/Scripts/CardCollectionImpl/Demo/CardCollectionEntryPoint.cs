using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class CardCollectionEntryPoint : MonoBehaviour
    {
        private CardCollectionModule _cardCollectionModule;
        private ICardPackProvider _cardPackProvider;
        private Exception _initializationException;

        private readonly UniTaskCompletionSource _initializationSource = new();

        public bool IsInitialized => _initializationSource.Task.Status == UniTaskStatus.Succeeded;
        public UniTask WaitForInitializationAsync() => _initializationSource.Task;

        public ICardCollectionModule CardCollectionModule => GetInitializedModule();
        public ICardCollectionUpdater CardCollectionUpdater => GetInitializedModule();
        public ICardCollectionReader CardCollectionReader => GetInitializedModule();
        public ICardCollectionPointsAccount CardCollectionPointsAccount => GetInitializedModule();
        public ICardPackProvider CardPackProvider => GetInitializedCardPackProvider();

        public event Action<Exception> OnInitializationFailed;

        private void Awake()
        {
            var ct = this.GetCancellationTokenOnDestroy();
            InitCardCollection(ct).Forget();
        }

        private CardCollectionModule GetInitializedModule()
        {
            if (_initializationException != null)
                throw new InvalidOperationException(
                    "CardCollection module initialization failed. See inner exception for details.",
                    _initializationException);

            if (!IsInitialized)
                throw new InvalidOperationException(
                    "CardCollection module is not yet initialized. " +
                    "Await WaitForInitializationAsync() before accessing the module.");

            return _cardCollectionModule;
        }

        private ICardPackProvider GetInitializedCardPackProvider()
        {
            if (_initializationException != null)
                throw new InvalidOperationException(
                    "CardCollection module initialization failed. See inner exception for details.",
                    _initializationException);

            if (!IsInitialized || _cardPackProvider == null)
                throw new InvalidOperationException(
                    "CardCollection module is not yet initialized. " +
                    "Await WaitForInitializationAsync() before accessing the module.");

            return _cardPackProvider;
        }

        private async UniTask InitCardCollection(CancellationToken ct)
        {
            try
            {
                const string testEventId = "test";
                
                _cardPackProvider = new JsonCardPackProvider();
                IEventCardsStorage cardsStorage = new JsonEventCardsStorage();
                ICardDefinitionProvider cardDefinitionProvider = new DefaultCardDefinitionProvider();
                ICardSelector cardSelector = new ProbabilityBasedCardSelector(PackRulesConfig.CreateDefaultRules());

                var config = new CardCollectionModuleConfig(_cardPackProvider, cardsStorage, cardDefinitionProvider, cardSelector, CardsCollectionPointsCalculator.Instance, testEventId);

                _cardCollectionModule = new CardCollectionModule(config);
                await _cardCollectionModule.InitializeAsync(ct);

                _cardCollectionModule.OnGroupCompleted += GroupCompletedHandler;
                
                _initializationSource.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception ex)
            {
                _initializationException = ex;
                _initializationSource.TrySetException(ex);
                OnInitializationFailed?.Invoke(ex);
            }
        }

        private void GroupCompletedHandler(CardGroupCompletedData groupCompletedData)
        {
            Debug.LogWarning($"Debug groupCompletedData.GroupId {groupCompletedData.GroupId}");
        }

        private void OnDestroy()
        {
            _cardCollectionModule.OnGroupCompleted -= GroupCompletedHandler;
        }
    }
}
