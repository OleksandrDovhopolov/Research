using System.Reflection;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionRewardHandlerTests
    {
        [Test]
        public void TryHandleCollectionCompleted_WhenRewardIdIsMissing_ReturnsTrue()
        {
            var rewardsReceiver = new FakeOfferRewardsReceiver();
            var rewardFactory = new FakeRewardDefinitionFactory();
            var handler = new CardCollectionRewardHandler(rewardsReceiver, null, rewardFactory);

            var config = AssetDatabase.LoadAssetAtPath<CardCollectionRewardsConfigSO>(
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
            var handler = new CardCollectionRewardHandler(rewardsReceiver, null, rewardFactory);

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

            public UniTask<bool> ReceiveRewardsAsync(CollectionRewardDefinition collectionRewardDefinition, CancellationToken ct = default)
            {
                ReceiveCallsCount++;
                return UniTask.FromResult(true);
            }
        }

        private sealed class FakeRewardDefinitionFactory : IRewardDefinitionFactory
        {
            public int CreateCollectionCallsCount { get; private set; }

            public CollectionRewardDefinition CreateFromGroupReward(CollectionCompletionRewardConfig collectionCompletionRewardConfig)
            {
                return new CardGroupCompletionReward();
            }

            public CollectionRewardDefinition CreateFromCollectionReward(FullCollectionRewardConfig fullCollectionRewardConfig = default)
            {
                CreateCollectionCallsCount++;
                return new FullCollectionReward();
            }
        }
    }
}
