using System.Collections.Generic;
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
            var rewardSpecProvider = new FakeRewardSpecProvider();
            
            //TODO moving file to different folders would cause fail
            var config = AssetDatabase.LoadAssetAtPath<CardCollectionRewardsConfigSO>(
                "Assets/CardsCollectionImpl/Data/Shared/CardCollectionRewardsConfig.asset");
            
            Assert.IsNotNull(config, "CardCollectionRewardsConfig asset not found at expected path.");
            
            var handler = new CardCollectionRewardHandler(config, rewardSpecProvider, rewardGrantService);
            
            //SetInitializedConfig(handler, config);

            //TODO EventId should be the same as in file CardCollectionRewardsConfig
            var result = handler.TryHandleCollectionCompleted(new CardCollectionCompletedData
            {
                EventId = "season_cards_001"
            }).GetAwaiter().GetResult();

            Assert.IsTrue(result);
            Assert.AreEqual(1, rewardSpecProvider.CreateCollectionCallsCount);
        }

        [Test]
        public void TryHandleCollectionCompleted_WhenRewardIdMatchesEventId_ReturnsTrueAndSendsReward()
        {
            var rewardGrantService = new FakeRewardGrantService();
            var rewardSpecProvider = new FakeRewardSpecProvider();
            
            var config = ScriptableObject.CreateInstance<CardCollectionRewardsConfigSO>();
            config.FullCollectionReward = new FullCollectionRewardConfig
            {
                RewardId = "event-1",
            };
            
            var handler = new CardCollectionRewardHandler(config, rewardSpecProvider, rewardGrantService);
            
            //SetInitializedConfig(handler, config);

            var result = handler.TryHandleCollectionCompleted(new CardCollectionCompletedData
            {
                EventId = "event-1"
            }).GetAwaiter().GetResult();

            Assert.IsTrue(result);
            Assert.AreEqual(1, rewardSpecProvider.CreateCollectionCallsCount);
        }

        private sealed class FakeRewardGrantService : IRewardGrantService
        {
            public int GrantCallsCount { get; private set; }

            public UniTask<bool> TryGrantAsync(RewardGrantRequest rewardRequest, CancellationToken ct = default)
            {
                GrantCallsCount++;
                return UniTask.FromResult(true);
            }

            public UniTask<bool> TryGrantAsync(List<RewardGrantRequest> rewardRequest, CancellationToken ct = default)
            {
                GrantCallsCount++;
                return UniTask.FromResult(true);
            }
        }

        private sealed class FakeRewardSpecProvider : IRewardSpecProvider
        {
            public int CreateCollectionCallsCount { get; private set; }

            public bool TryGet(string rewardId, out RewardSpec spec)
            {
                spec = null;
                CreateCollectionCallsCount++;
                return true;
            }
        }
    }
}
