using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using EventOrchestration.Models;
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

        [Test]
        public void InitializeAsync_UsesCardCollectionEventModelFactory()
        {
            var controller = new CardCollectionLiveOpsController(
                new CardCollectionEventModelFactory(),
                null,
                null,
                null,
                null);
            var schedule = new ScheduleItem
            {
                Id = "winter",
                EventType = "CardCollection",
                StreamId = "card_collection_seasons",
                StartTimeUtc = DateTimeOffset.Parse("2026-04-16T08:39:03Z"),
                EndTimeUtc = DateTimeOffset.Parse("2026-04-27T22:25:43Z"),
                CustomParams = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["collectionName"] = "Winter Collection",
                    ["eventConfigAddress"] = "event_winter_collection_config"
                }
            };
            var state = new EventStateData
            {
                ScheduleItemId = "winter",
                State = EventInstanceState.Pending,
                UpdatedAtUtc = DateTimeOffset.Parse("2026-04-24T10:00:00Z"),
            };

            controller.InitializeAsync(schedule, state, CancellationToken.None).GetAwaiter().GetResult();

            var model = (CardCollectionEventModel)GetPrivateBaseProperty(controller, "CurrentModel");
            Assert.That(model.CollectionName, Is.EqualTo("Winter Collection"));
            Assert.That(model.EventConfigAddress, Is.EqualTo("event_winter_collection_config"));
        }

        private static CardCollectionSession CreateSession()
        {
            return (CardCollectionSession)FormatterServices.GetUninitializedObject(typeof(CardCollectionSession));
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Field '{fieldName}' was not found.");
            field!.SetValue(target, value);
        }

        private static object GetPrivateBaseProperty(object target, string propertyName)
        {
            var property = target.GetType().BaseType?.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(property, $"Property '{propertyName}' was not found.");
            return property!.GetValue(target);
        }
    }
}
