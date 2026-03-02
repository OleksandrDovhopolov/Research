using System;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace core
{
    public class CardCollectionEntryPoint : MonoBehaviour
    {
        [SerializeField] private Starter _starter;
        
        private CardCollectionModule _cardCollectionModule;
        private ICardPackProvider _cardPackProvider;
        private CardCollectionRewardHandler _rewardHandler;
        private Exception _initializationException;
        private readonly UniTaskCompletionSource _rewardHandlerInitializationSource = new();

        private readonly UniTaskCompletionSource _initializationSource = new();

        public bool IsInitialized => _initializationSource.Task.Status == UniTaskStatus.Succeeded;
        public UniTask WaitForInitializationAsync() => _initializationSource.Task;

        public ICardCollectionModule CardCollectionModule => GetInitializedModule();
        public ICardCollectionUpdater CardCollectionUpdater => GetInitializedModule();
        public ICardCollectionReader CardCollectionReader => GetInitializedModule();
        public ICardCollectionPointsAccount CardCollectionPointsAccount => GetInitializedModule();
        public ICardPackProvider CardPackProvider => GetInitializedCardPackProvider();

        public event Action<Exception> OnInitializationFailed;

        public async UniTask InitializeRewardHandlerAsync(IOfferRewardsReceiver rewardsReceiver, IRewardDefinitionFactory rewardDefinitionFactory, CancellationToken ct = default)
        {
            try
            {
                //TODO combine OfferRewardsReceiver with init in Starter
                _rewardHandler = new CardCollectionRewardHandler(rewardsReceiver, rewardDefinitionFactory);
                await _rewardHandler.InitializeAsync(ct);
                _rewardHandlerInitializationSource.TrySetResult();
            }
            catch (OperationCanceledException)
            {
                _rewardHandlerInitializationSource.TrySetCanceled(ct);
                throw;
            }
            catch (Exception ex)
            {
                _rewardHandlerInitializationSource.TrySetException(ex);
                throw;
            }
        }

        public UniTask WaitForRewardHandlerInitializationAsync(CancellationToken ct = default)
        {
            return _rewardHandlerInitializationSource.Task.AttachExternalCancellation(ct);
        }

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
                _cardCollectionModule.OnCollectionCompleted += CollectionCompletedHandler;
                
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
            if (_rewardHandler == null)
            {
                Debug.LogWarning($"Failed to handler reward event. CardCollectionRewardHandler is null. Group id = {groupCompletedData.GroupId}");
                return;
            }

            _rewardHandler.TryHandleGroupCompleted(groupCompletedData);
        }

        private void CollectionCompletedHandler(CardCollectionCompletedData collectionCompletedData)
        {
            if (_rewardHandler == null)
            {
                Debug.LogWarning("Failed to handle collection-completed reward event. CardCollectionRewardHandler is null.");
                return;
            }

            _rewardHandler.TryHandleCollectionCompleted(collectionCompletedData);
        }

        private void OnDestroy()
        {
            if (_cardCollectionModule != null)
            {
                _cardCollectionModule.OnGroupCompleted -= GroupCompletedHandler;
                _cardCollectionModule.OnCollectionCompleted -= CollectionCompletedHandler;
            }
        }
    }
}
