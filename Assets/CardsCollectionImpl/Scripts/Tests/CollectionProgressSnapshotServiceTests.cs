using CardCollection.Core;
using System.Collections.Generic;
using NUnit.Framework;

namespace CardCollectionImpl
{
    public class CollectionProgressSnapshotBuilderTests
    {
        [Test]
        public void Build_WithNullData_ReturnsDefaultSnapshot()
        {
            var builder = CreateBuilder(new Dictionary<string, (int collected, int total)>());

            var snapshot = builder.Build(null);
            Assert.AreEqual(0, snapshot.CollectedAmount);
            Assert.AreEqual(0, snapshot.TotalAmount);
            Assert.IsNull(snapshot.GroupProgress);
        }

        [Test]
        public void Build_SetsTotalAndCollectedAmounts()
        {
            var builder = CreateBuilder(new Dictionary<string, (int collected, int total)>());
            var data = new EventCardsSaveData();
            data.Cards.Add(new CardProgressData { CardId = "a1", IsUnlocked = true });
            data.Cards.Add(new CardProgressData { CardId = "a2", IsUnlocked = false });

            var snapshot = builder.Build(data);

            Assert.AreEqual(1, snapshot.CollectedAmount);
            Assert.AreEqual(2, snapshot.TotalAmount);
        }

        [Test]
        public void Build_BuildsPerGroupProgress()
        {
            var builder = CreateBuilder(new Dictionary<string, (int collected, int total)>
            {
                ["g1"] = (1, 2)
            });
            var data = new EventCardsSaveData();
            data.Cards.Add(new CardProgressData { CardId = "card-g1-1", IsUnlocked = true });
            data.Cards.Add(new CardProgressData { CardId = "card-g1-2", IsUnlocked = false });

            var snapshot = builder.Build(data);

            Assert.AreEqual(1, snapshot.GroupProgress.Count);
            Assert.AreEqual("g1", snapshot.GroupProgress[0].GroupType);
            Assert.AreEqual("Group One", snapshot.GroupProgress[0].GroupName);
            Assert.AreEqual(1, snapshot.GroupProgress[0].CollectedAmount);
            Assert.AreEqual(2, snapshot.GroupProgress[0].TotalAmount);
        }

        private static CollectionProgressSnapshotBuilder CreateBuilder(
            Dictionary<string, (int collected, int total)> byGroupType)
        {
            var cache = new FakeCardCollectionCacheService(byGroupType);
            var groups = new List<CardCollectionGroupConfig>
            {
                new()
                {
                    groupType = "g1",
                    groupName = "Group One"
                }
            };

            return new CollectionProgressSnapshotBuilder(cache, groups);
        }

        private sealed class FakeCardCollectionCacheService : ICardCollectionCacheService
        {
            private readonly Dictionary<string, (int collected, int total)> _byGroupType;

            public FakeCardCollectionCacheService(Dictionary<string, (int collected, int total)> byGroupType)
            {
                _byGroupType = byGroupType ?? new Dictionary<string, (int collected, int total)>();
            }

            public void Initialize(IReadOnlyList<CardConfig> configs) { }

            public IEnumerable<CardProgressData> GetCardsByGroupType(EventCardsSaveData saveData, string groupType)
            {
                return new List<CardProgressData>();
            }

            public int GetGroupAmount(EventCardsSaveData saveData, string groupType)
            {
                return _byGroupType.TryGetValue(groupType, out var value) ? value.total : 0;
            }

            public int GetCollectedGroupAmount(EventCardsSaveData saveData, string groupType)
            {
                return _byGroupType.TryGetValue(groupType, out var value) ? value.collected : 0;
            }

            public List<NewCardDisplayData> ToNewCardDisplayData(List<CardProgressData> cardsData)
            {
                return new List<NewCardDisplayData>();
            }
        }
    }
}
