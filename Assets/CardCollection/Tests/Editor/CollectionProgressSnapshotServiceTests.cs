using CardCollection.Core;
using NUnit.Framework;

namespace CardCollection.Tests
{
    public class CollectionProgressSnapshotServiceTests
    {
        [Test]
        public void TryGetSnapshot_WithoutAnySnapshot_ReturnsFalse()
        {
            var service = new CollectionProgressSnapshotService();

            var hasSnapshot = service.TryGetSnapshot(out var snapshot);

            Assert.False(hasSnapshot);
            Assert.AreEqual(0, snapshot.CollectedAmount);
            Assert.AreEqual(0, snapshot.TotalAmount);
        }

        [Test]
        public void SetSnapshot_ThenTryGetSnapshot_ReturnsStoredValues()
        {
            var service = new CollectionProgressSnapshotService();

            service.SetSnapshot(10, 100);
            var hasSnapshot = service.TryGetSnapshot(out var snapshot);

            Assert.True(hasSnapshot);
            Assert.AreEqual(10, snapshot.CollectedAmount);
            Assert.AreEqual(100, snapshot.TotalAmount);
        }

        [Test]
        public void SetSnapshot_WhenCalledAgain_OverwritesPreviousValues()
        {
            var service = new CollectionProgressSnapshotService();
            service.SetSnapshot(10, 100);

            service.SetSnapshot(20, 100);
            var hasSnapshot = service.TryGetSnapshot(out var snapshot);

            Assert.True(hasSnapshot);
            Assert.AreEqual(20, snapshot.CollectedAmount);
            Assert.AreEqual(100, snapshot.TotalAmount);
        }
    }
}
