using System.Reflection;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace core
{
    public class CardCollectionRewardHandlerTests
    {
        [Test]
        public void TryHandleCollectionCompleted_WhenRewardIdIsMissing_ReturnsTrue()
        {
            var rewardsReceiver = new FakeOfferRewardsReceiver();
            var rewardFactory = new FakeRewardDefinitionFactory();
            var handler = new CardCollectionRewardHandler(rewardsReceiver, rewardFactory);

            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<CardCollectionRewardsConfigSO>(
                "Assets/CardsCollectionImpl/Scripts/Rewards/CardCollectionRewardsConfig.asset");
            
            Assert.IsNotNull(config, "CardCollectionRewardsConfig asset not found at expected path.");
            
            SetInitializedConfig(handler, config);

            var result = handler.TryHandleCollectionCompleted(new CardCollectionCompletedData
            {
                EventId = "test"
            });

            Assert.IsTrue(result);
            Assert.AreEqual(1, rewardsReceiver.ReceiveCallsCount);
            Assert.AreEqual(1, rewardFactory.CreateCollectionCallsCount);
        }

        [Test]
        public void TryHandleCollectionCompleted_WhenRewardIdMatchesEventId_ReturnsTrueAndSendsReward()
        {
            var rewardsReceiver = new FakeOfferRewardsReceiver();
            var rewardFactory = new FakeRewardDefinitionFactory();
            var handler = new CardCollectionRewardHandler(rewardsReceiver, rewardFactory);

            var config = ScriptableObject.CreateInstance<CardCollectionRewardsConfigSO>();
            config.FullCollectionReward = new FullCollectionRewardConfig
            {
                RewardId = "event-1",
            };

            SetInitializedConfig(handler, config);

            var result = handler.TryHandleCollectionCompleted(new CardCollectionCompletedData
            {
                EventId = "event-1"
            });

            Assert.IsTrue(result);
            Assert.AreEqual(1, rewardsReceiver.ReceiveCallsCount);
            Assert.AreEqual(1, rewardFactory.CreateCollectionCallsCount);
        }

        private static void SetInitializedConfig(CardCollectionRewardHandler handler, CardCollectionRewardsConfigSO config)
        {
            var type = typeof(CardCollectionRewardHandler);
            type.GetField("_isInitialized", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(handler, true);
            type.GetField("_cardCollectionRewardsConfigSo", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(handler, config);
        }

        private sealed class FakeOfferRewardsReceiver : IOfferRewardsReceiver
        {
            public int ReceiveCallsCount { get; private set; }

            public UniTask<bool> ReceiveRewardsAsync(CardCollectionImpl.CollectionRewardDefinition collectionRewardDefinition, System.Threading.CancellationToken ct = default)
            {
                ReceiveCallsCount++;
                return UniTask.FromResult(true);
            }
        }

        private sealed class FakeRewardDefinitionFactory : IRewardDefinitionFactory
        {
            public int CreateCollectionCallsCount { get; private set; }

            public CardCollectionImpl.CollectionRewardDefinition CreateFromGroupReward(CollectionCompletionRewardConfig collectionCompletionRewardConfig)
            {
                return new CardGroupCompletionReward();
            }

            public CardCollectionImpl.CollectionRewardDefinition CreateFromCollectionReward(FullCollectionRewardConfig fullCollectionRewardConfig = default)
            {
                CreateCollectionCallsCount++;
                return new FullCollectionReward();
            }

            public CardCollectionImpl.CollectionRewardDefinition CreateFromExchangePack(ExchangePackEntry exchangePackEntry, System.Collections.Generic.IReadOnlyCollection<CardPack> cardPacks)
            {
                return new DuplicatePointsChestOffer();
            }
        }
    }
}
