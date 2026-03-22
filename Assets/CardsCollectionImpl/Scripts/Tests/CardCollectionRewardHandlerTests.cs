using System.Reflection;
using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Rewards;
using UnityEditor;
using UnityEngine;

namespace CardCollectionImpl
{
    public class CardCollectionRewardHandlerTests
    {
        /*
         * bad test quality
         * TODO fix this test
         */
        [Test]
        public void TryHandleCollectionCompleted_WhenRewardIdIsMissing_ReturnsTrue()
        {
            var rewardGrantService = new FakeRewardGrantService();
            var rewardFactory = new FakeRewardDefinitionFactory();
            
            //TODO moving file to different folders would cause fail
            var config = AssetDatabase.LoadAssetAtPath<CardCollectionRewardsConfigSO>(
                "Assets/CardsCollectionImpl/Data/season_cards_001/CardCollectionRewardsConfig.asset");
            
            Assert.IsNotNull(config, "CardCollectionRewardsConfig asset not found at expected path.");
            
            var handler = new CardCollectionRewardHandler(config, rewardGrantService, rewardFactory);
            
            //SetInitializedConfig(handler, config);

            //TODO EventId should be the same as in file CardCollectionRewardsConfig
            var result = handler.TryHandleCollectionCompleted(new CardCollectionCompletedData
            {
                EventId = "season_cards_001"
            }).GetAwaiter().GetResult();

            Assert.IsTrue(result);
            Assert.AreEqual(1, rewardFactory.CreateCollectionCallsCount);
        }

        [Test]
        public void TryHandleCollectionCompleted_WhenRewardIdMatchesEventId_ReturnsTrueAndSendsReward()
        {
            var rewardGrantService = new FakeRewardGrantService();
            var rewardFactory = new FakeRewardDefinitionFactory();
            
            var config = ScriptableObject.CreateInstance<CardCollectionRewardsConfigSO>();
            config.FullCollectionReward = new FullCollectionRewardConfig
            {
                RewardId = "event-1",
            };
            
            var handler = new CardCollectionRewardHandler(config, rewardGrantService, rewardFactory);
            
            //SetInitializedConfig(handler, config);

            var result = handler.TryHandleCollectionCompleted(new CardCollectionCompletedData
            {
                EventId = "event-1"
            }).GetAwaiter().GetResult();

            Assert.IsTrue(result);
            Assert.AreEqual(1, rewardFactory.CreateCollectionCallsCount);
        }

        private static void SetInitializedConfig(CardCollectionRewardHandler handler, CardCollectionRewardsConfigSO config)
        {
            var type = typeof(CardCollectionRewardHandler);
            type.GetField("_isInitialized", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(handler, true);
            type.GetField("_cardCollectionRewardsConfigSo", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(handler, config);
        }

        private sealed class FakeRewardGrantService : IRewardGrantService
        {
            public int GrantCallsCount { get; private set; }

            public UniTask<bool> TryGrantAsync(RewardGrantRequest rewardRequest, CancellationToken ct = default)
            {
                GrantCallsCount++;
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

            public CollectionRewardDefinition CreateFromOfferReward(string offerPackId)
            {
                CreateCollectionCallsCount++;
                return new DuplicatePointsChestOffer();
            }
        }
    }
}
