using System;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

namespace CardCollectionImpl
{
    public class CardCollectionSessionLifecycleTests
    {
        [Test]
        public void StopAsync_WhenTokenAlreadyCanceled_ThrowsOperationCanceledException()
        {
            var session = CreateSession();
            SetPrivateField(session, "_isStarted", true);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(
                () => session.StopAsync(cts.Token).GetAwaiter().GetResult());
        }

        [Test]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var session = CreateSession();

            session.Dispose();
            session.Dispose();

            session.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            Assert.Pass();
        }

        [Test]
        public void StopAsync_AfterDispose_DoesNotThrow()
        {
            var session = CreateSession();
            session.Dispose();

            session.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            Assert.Pass();
        }

        private static CardCollectionSession CreateSession()
        {
            return new CardCollectionSession(
                uiManager: null,
                context: null,
                facade: null,
                hudPresenter: null,
                rewardHandler: null,
                rewardsConfig: null,
                inventoryIntegration: null,
                snapshotService: null);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Field '{fieldName}' was not found.");
            field!.SetValue(target, value);
        }
    }
}
