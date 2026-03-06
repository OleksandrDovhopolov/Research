using CardCollection.Core;
using NUnit.Framework;
using Infrastructure;

namespace CardCollectionImpl
{
    public class CollectionProgressSnapshotServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            CardGroupsConfigStorage.Instance.Data.Clear();
            CardCollectionConfigStorage.Instance.Data.Clear();
        }

        [Test]
        public void TryGetSnapshot_WithoutSet_ReturnsFalse()
        {
            var service = new CollectionProgressSnapshotService();

            var hasSnapshot = service.TryGetSnapshot(out _);

            Assert.False(hasSnapshot);
        }

        [Test]
        public void SetSnapshot_SetsTotalAndCollectedAmounts()
        {
            var service = new CollectionProgressSnapshotService();
            var data = new EventCardsSaveData();
            data.Cards.Add(new CardProgressData { CardId = "a1", IsUnlocked = true });
            data.Cards.Add(new CardProgressData { CardId = "a2", IsUnlocked = false });

            service.SetSnapshot(data);
            var hasSnapshot = service.TryGetSnapshot(out var snapshot);

            Assert.True(hasSnapshot);
            Assert.AreEqual(1, snapshot.CollectedAmount);
            Assert.AreEqual(2, snapshot.TotalAmount);
        }

        [Test]
        public void SetSnapshot_BuildsPerGroupProgress()
        {
            CardGroupsConfigStorage.Instance.Data.Add(new CardGroupsConfig
            {
                GroupType = "g1",
                GroupName = "Group One"
            });

            CardCollectionConfigStorage.Instance.Data.Add(new CardCollectionConfig
            {
                Id = "card-g1-1",
                GroupType = "g1"
            });
            CardCollectionConfigStorage.Instance.Data.Add(new CardCollectionConfig
            {
                Id = "card-g1-2",
                GroupType = "g1"
            });

            var service = new CollectionProgressSnapshotService();
            var data = new EventCardsSaveData();
            data.Cards.Add(new CardProgressData { CardId = "card-g1-1", IsUnlocked = true });
            data.Cards.Add(new CardProgressData { CardId = "card-g1-2", IsUnlocked = false });

            service.SetSnapshot(data);
            service.TryGetSnapshot(out var snapshot);

            Assert.AreEqual(1, snapshot.GroupProgress.Count);
            Assert.AreEqual("g1", snapshot.GroupProgress[0].GroupType);
            Assert.AreEqual("Group One", snapshot.GroupProgress[0].GroupName);
            Assert.AreEqual(1, snapshot.GroupProgress[0].CollectedAmount);
            Assert.AreEqual(2, snapshot.GroupProgress[0].TotalAmount);
        }
    }
}
