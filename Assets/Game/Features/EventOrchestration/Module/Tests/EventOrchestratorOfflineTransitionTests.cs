using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Abstractions;
using EventOrchestration.Core;
using EventOrchestration.Models;
using NUnit.Framework;

namespace EventOrchestration.Tests.Editor
{
    public sealed class EventOrchestratorOfflineTransitionTests
    {
        [Test]
        public void TickAsync_WhenNowInsideEventWindow_StartsPendingEvent()
        {
            var now = new DateTimeOffset(2026, 4, 11, 13, 0, 0, TimeSpan.Zero);
            var schedule = new List<ScheduleItem>
            {
                new()
                {
                    Id = "event1",
                    EventType = "CardCollection1",
                    StreamId = "main",
                    Priority = 1,
                    StartTimeUtc = now.AddMinutes(-5),
                    EndTimeUtc = now.AddHours(1),
                },
            };

            var restoredStates = new Dictionary<string, EventStateData>
            {
                ["event1"] = new()
                {
                    ScheduleItemId = "event1",
                    State = EventInstanceState.Pending,
                    Version = 1,
                    UpdatedAtUtc = now,
                },
            };

            var callOrder = new List<string>();
            var event1Controller = new FakeEventController("CardCollection1", callOrder);

            var orchestrator = CreateOrchestrator(
                now,
                schedule,
                restoredStates,
                event1Controller,
                out var stateStore,
                out _);

            var startedCount = 0;
            orchestrator.OnEventStarted += _ => startedCount++;

            orchestrator.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            orchestrator.TickAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, event1Controller.OnStartCalls);
            Assert.AreEqual(1, startedCount);
            Assert.That(stateStore.LastSavedStates.TryGetValue("event1", out var event1State), Is.True);
            Assert.AreEqual(EventInstanceState.Active, event1State.State);
            Assert.IsTrue(event1State.StartInvoked);
        }

        [Test]
        public void TickAsync_WhenNowBeforeStart_DoesNotStartEvent()
        {
            var now = new DateTimeOffset(2026, 4, 11, 13, 0, 0, TimeSpan.Zero);
            var schedule = new List<ScheduleItem>
            {
                new()
                {
                    Id = "event1",
                    EventType = "CardCollection1",
                    StreamId = "main",
                    Priority = 1,
                    StartTimeUtc = now.AddMinutes(5),
                    EndTimeUtc = now.AddHours(1),
                },
            };

            var restoredStates = new Dictionary<string, EventStateData>
            {
                ["event1"] = new()
                {
                    ScheduleItemId = "event1",
                    State = EventInstanceState.Pending,
                    Version = 1,
                    UpdatedAtUtc = now,
                },
            };

            var callOrder = new List<string>();
            var event1Controller = new FakeEventController("CardCollection1", callOrder);

            var orchestrator = CreateOrchestrator(
                now,
                schedule,
                restoredStates,
                event1Controller,
                out var stateStore,
                out _);

            orchestrator.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            orchestrator.TickAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.Zero(event1Controller.OnStartCalls);
            Assert.Zero(event1Controller.OnUpdateCalls);
            Assert.IsEmpty(callOrder);
            Assert.That(stateStore.LastSavedStates.TryGetValue("event1", out var event1State), Is.True);
            Assert.AreEqual(EventInstanceState.Pending, event1State.State);
            Assert.IsFalse(event1State.StartInvoked);
        }

        [Test]
        public void TickAsync_WhenPreviousEventExpiredOffline_EndsPreviousThenStartsNextInSameStream()
        {
            var now = new DateTimeOffset(2026, 4, 11, 13, 0, 0, TimeSpan.Zero);
            var schedule = new List<ScheduleItem>
            {
                new()
                {
                    Id = "event1",
                    EventType = "CardCollection1",
                    StreamId = "main",
                    Priority = 1,
                    StartTimeUtc = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
                    EndTimeUtc = new DateTimeOffset(2026, 4, 10, 0, 0, 0, TimeSpan.Zero),
                },
                new()
                {
                    Id = "event2",
                    EventType = "CardCollection2",
                    StreamId = "main",
                    Priority = 1,
                    StartTimeUtc = new DateTimeOffset(2026, 4, 10, 0, 0, 0, TimeSpan.Zero),
                    EndTimeUtc = new DateTimeOffset(2026, 4, 20, 0, 0, 0, TimeSpan.Zero),
                },
            };

            var restoredStates = new Dictionary<string, EventStateData>
            {
                ["event1"] = new()
                {
                    ScheduleItemId = "event1",
                    State = EventInstanceState.Active,
                    Version = 1,
                    UpdatedAtUtc = now,
                    StartInvoked = true,
                    EndInvoked = false,
                    SettlementInvoked = false,
                },
                ["event2"] = new()
                {
                    ScheduleItemId = "event2",
                    State = EventInstanceState.Pending,
                    Version = 1,
                    UpdatedAtUtc = now,
                },
            };

            var callOrder = new List<string>();
            var event1Controller = new FakeEventController("CardCollection1", callOrder);
            var event2Controller = new FakeEventController("CardCollection2", callOrder);

            var orchestrator = CreateOrchestrator(
                now,
                schedule,
                restoredStates,
                event1Controller,
                event2Controller,
                out var stateStore,
                out _);

            orchestrator.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            orchestrator.TickAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, event1Controller.OnEndCalls);
            Assert.AreEqual(1, event1Controller.ExecuteSettlementCalls);
            Assert.AreEqual(1, event2Controller.OnStartCalls);

            var firstEvent1EndIndex = callOrder.IndexOf("event1.OnEnd");
            var firstEvent1SettleIndex = callOrder.IndexOf("event1.ExecuteSettlement");
            var firstEvent2StartIndex = callOrder.IndexOf("event2.OnStart");
            Assert.That(firstEvent1EndIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(firstEvent1SettleIndex, Is.GreaterThan(firstEvent1EndIndex));
            Assert.That(firstEvent2StartIndex, Is.GreaterThan(firstEvent1SettleIndex));

            Assert.That(stateStore.LastSavedStates.TryGetValue("event1", out _), Is.False);
            Assert.That(stateStore.LastSavedStates.TryGetValue("event2", out var event2State), Is.True);
            Assert.AreEqual(EventInstanceState.Active, event2State.State);
            Assert.IsTrue(event2State.StartInvoked);
        }

        [Test]
        public void TickAsync_WhenActiveEventExpired_EndsAndSettlesAndCompletes()
        {
            var now = new DateTimeOffset(2026, 4, 11, 13, 0, 0, TimeSpan.Zero);
            var schedule = new List<ScheduleItem>
            {
                new()
                {
                    Id = "event1",
                    EventType = "CardCollection1",
                    StreamId = "main",
                    Priority = 1,
                    StartTimeUtc = now.AddDays(-5),
                    EndTimeUtc = now.AddMinutes(-1),
                },
            };

            var restoredStates = new Dictionary<string, EventStateData>
            {
                ["event1"] = new()
                {
                    ScheduleItemId = "event1",
                    State = EventInstanceState.Active,
                    Version = 1,
                    UpdatedAtUtc = now,
                    StartInvoked = true,
                },
            };

            var callOrder = new List<string>();
            var event1Controller = new FakeEventController("CardCollection1", callOrder);

            var orchestrator = CreateOrchestrator(
                now,
                schedule,
                restoredStates,
                event1Controller,
                out var stateStore,
                out _);

            var completedCount = 0;
            orchestrator.OnEventCompleted += _ => completedCount++;

            orchestrator.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            orchestrator.TickAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, event1Controller.OnEndCalls);
            Assert.AreEqual(1, event1Controller.ExecuteSettlementCalls);
            Assert.AreEqual(1, completedCount);

            Assert.That(callOrder.IndexOf("event1.OnEnd"), Is.GreaterThanOrEqualTo(0));
            Assert.That(callOrder.IndexOf("event1.ExecuteSettlement"), Is.GreaterThan(callOrder.IndexOf("event1.OnEnd")));

            Assert.That(stateStore.LastSavedStates.TryGetValue("event1", out _), Is.False);
        }

        [Test]
        public void TickAsync_WhenOnUpdateThrows_MarksEventFailed()
        {
            var now = new DateTimeOffset(2026, 4, 11, 13, 0, 0, TimeSpan.Zero);
            var schedule = new List<ScheduleItem>
            {
                new()
                {
                    Id = "event1",
                    EventType = "CardCollection1",
                    StreamId = "main",
                    Priority = 1,
                    StartTimeUtc = now.AddDays(-1),
                    EndTimeUtc = now.AddHours(2),
                },
            };

            var restoredStates = new Dictionary<string, EventStateData>
            {
                ["event1"] = new()
                {
                    ScheduleItemId = "event1",
                    State = EventInstanceState.Active,
                    Version = 1,
                    UpdatedAtUtc = now,
                    StartInvoked = true,
                },
            };

            var callOrder = new List<string>();
            var event1Controller = new FakeEventController("CardCollection1", callOrder)
            {
                ThrowOnUpdate = new InvalidOperationException("update failed"),
            };

            var orchestrator = CreateOrchestrator(
                now,
                schedule,
                restoredStates,
                event1Controller,
                out var stateStore,
                out var telemetry);

            orchestrator.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            orchestrator.TickAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(stateStore.LastSavedStates.TryGetValue("event1", out var event1State), Is.True);
            Assert.AreEqual(EventInstanceState.Failed, event1State.State);
            Assert.That(event1State.LastError, Does.Contain("UpdateOrEnd"));
            Assert.AreEqual(1, telemetry.FailureCalls);
            Assert.AreEqual("UpdateOrEnd", telemetry.LastFailureStage);
        }

        [Test]
        public void TickAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
        {
            var now = new DateTimeOffset(2026, 4, 11, 13, 0, 0, TimeSpan.Zero);
            var schedule = new List<ScheduleItem>
            {
                new()
                {
                    Id = "event1",
                    EventType = "CardCollection1",
                    StreamId = "main",
                    Priority = 1,
                    StartTimeUtc = now.AddMinutes(-1),
                    EndTimeUtc = now.AddHours(1),
                },
            };

            var restoredStates = new Dictionary<string, EventStateData>
            {
                ["event1"] = new()
                {
                    ScheduleItemId = "event1",
                    State = EventInstanceState.Pending,
                    Version = 1,
                    UpdatedAtUtc = now,
                },
            };

            var callOrder = new List<string>();
            var event1Controller = new FakeEventController("CardCollection1", callOrder);

            var orchestrator = CreateOrchestrator(
                now,
                schedule,
                restoredStates,
                event1Controller,
                out _,
                out _);

            orchestrator.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(
                () => orchestrator.TickAsync(cts.Token).GetAwaiter().GetResult());
        }

        [Test]
        public void TickAsync_WhenFirstEventCompletesAndSecondIsReady_StartsSecondInSameTick()
        {
            var now = new DateTimeOffset(2026, 4, 11, 13, 0, 0, TimeSpan.Zero);
            var schedule = new List<ScheduleItem>
            {
                new()
                {
                    Id = "event1",
                    EventType = "CardCollection1",
                    StreamId = "main",
                    Priority = 1,
                    StartTimeUtc = now.AddDays(-2),
                    EndTimeUtc = now.AddMinutes(-1),
                },
                new()
                {
                    Id = "event2",
                    EventType = "CardCollection2",
                    StreamId = "main",
                    Priority = 1,
                    StartTimeUtc = now.AddMinutes(-1),
                    EndTimeUtc = now.AddDays(1),
                },
            };

            var restoredStates = new Dictionary<string, EventStateData>
            {
                ["event1"] = new()
                {
                    ScheduleItemId = "event1",
                    State = EventInstanceState.Active,
                    Version = 1,
                    UpdatedAtUtc = now,
                    StartInvoked = true,
                },
                ["event2"] = new()
                {
                    ScheduleItemId = "event2",
                    State = EventInstanceState.Pending,
                    Version = 1,
                    UpdatedAtUtc = now,
                },
            };

            var callOrder = new List<string>();
            var event1Controller = new FakeEventController("CardCollection1", callOrder);
            var event2Controller = new FakeEventController("CardCollection2", callOrder);

            var orchestrator = CreateOrchestrator(
                now,
                schedule,
                restoredStates,
                event1Controller,
                event2Controller,
                out var stateStore,
                out _);

            orchestrator.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            orchestrator.TickAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, event1Controller.OnEndCalls);
            Assert.AreEqual(1, event1Controller.ExecuteSettlementCalls);
            Assert.AreEqual(1, event2Controller.OnStartCalls);

            var firstEvent1SettleIndex = callOrder.IndexOf("event1.ExecuteSettlement");
            var firstEvent2StartIndex = callOrder.IndexOf("event2.OnStart");
            Assert.That(firstEvent1SettleIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(firstEvent2StartIndex, Is.GreaterThan(firstEvent1SettleIndex));

            Assert.That(stateStore.LastSavedStates.TryGetValue("event1", out _), Is.False);
            Assert.That(stateStore.LastSavedStates.TryGetValue("event2", out var event2State), Is.True);
            Assert.AreEqual(EventInstanceState.Active, event2State.State);
        }

        [Test]
        public void InitializeAndTick_WhenExpiredEventIsPending_DoesNotInvokeControllerEndFlow()
        {
            var now = new DateTimeOffset(2026, 4, 11, 13, 0, 0, TimeSpan.Zero);
            var schedule = new List<ScheduleItem>
            {
                new()
                {
                    Id = "event1",
                    EventType = "CardCollection1",
                    StreamId = "main",
                    Priority = 1,
                    StartTimeUtc = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
                    EndTimeUtc = new DateTimeOffset(2026, 4, 10, 0, 0, 0, TimeSpan.Zero),
                },
            };

            var restoredStates = new Dictionary<string, EventStateData>
            {
                ["event1"] = new()
                {
                    ScheduleItemId = "event1",
                    State = EventInstanceState.Pending,
                    Version = 1,
                    UpdatedAtUtc = now,
                },
            };

            var callOrder = new List<string>();
            var event1Controller = new FakeEventController("CardCollection1", callOrder);

            var orchestrator = CreateOrchestrator(
                now,
                schedule,
                restoredStates,
                event1Controller,
                out var stateStore,
                out _);

            orchestrator.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
            orchestrator.TickAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.Zero(event1Controller.OnEndCalls);
            Assert.Zero(event1Controller.ExecuteSettlementCalls);
            Assert.IsEmpty(callOrder);

            Assert.That(stateStore.LastSavedStates.TryGetValue("event1", out _), Is.False);
        }

        private static EventOrchestrator CreateOrchestrator(
            DateTimeOffset now,
            IReadOnlyList<ScheduleItem> schedule,
            Dictionary<string, EventStateData> restoredStates,
            FakeEventController event1Controller,
            out FakeStateStore stateStore,
            out FakeTelemetry telemetry)
        {
            return CreateOrchestrator(now, schedule, restoredStates, new[] { event1Controller }, out stateStore, out telemetry);
        }

        private static EventOrchestrator CreateOrchestrator(
            DateTimeOffset now,
            IReadOnlyList<ScheduleItem> schedule,
            Dictionary<string, EventStateData> restoredStates,
            FakeEventController event1Controller,
            FakeEventController event2Controller,
            out FakeStateStore stateStore,
            out FakeTelemetry telemetry)
        {
            return CreateOrchestrator(now, schedule, restoredStates, new[] { event1Controller, event2Controller }, out stateStore, out telemetry);
        }

        private static EventOrchestrator CreateOrchestrator(
            DateTimeOffset now,
            IReadOnlyList<ScheduleItem> schedule,
            Dictionary<string, EventStateData> restoredStates,
            IReadOnlyCollection<FakeEventController> controllers,
            out FakeStateStore stateStore,
            out FakeTelemetry telemetry)
        {
            var scheduleProvider = new FakeScheduleProvider(schedule);
            var scheduleValidator = new FakeScheduleValidator();
            var eventRegistry = new FakeEventRegistry(controllers);
            var clock = new FakeClock(now);
            stateStore = new FakeStateStore(restoredStates);
            telemetry = new FakeTelemetry();

            return new EventOrchestrator(scheduleProvider, scheduleValidator, eventRegistry, clock, stateStore, telemetry);
        }

        private sealed class FakeScheduleProvider : IScheduleProvider
        {
            private readonly IReadOnlyList<ScheduleItem> _schedule;

            public FakeScheduleProvider(IReadOnlyList<ScheduleItem> schedule)
            {
                _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            }

            public UniTask<IReadOnlyList<ScheduleItem>> LoadAsync(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(_schedule);
            }
        }

        private sealed class FakeScheduleValidator : IScheduleValidator
        {
            public UniTask<IReadOnlyList<string>> ValidateAsync(IReadOnlyList<ScheduleItem> items, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult((IReadOnlyList<string>)Array.Empty<string>());
            }
        }

        private sealed class FakeEventRegistry : IEventRegistry
        {
            private readonly Dictionary<string, IEventController> _controllers;

            public FakeEventRegistry(IEnumerable<IEventController> controllers)
            {
                _controllers = controllers.ToDictionary(controller => controller.EventType, controller => controller, StringComparer.Ordinal);
            }

            public void Register(IEventController controller)
            {
                if (controller == null)
                    throw new ArgumentNullException(nameof(controller));

                _controllers[controller.EventType] = controller;
            }

            public bool TryGet(string eventType, out IEventController controller)
            {
                return _controllers.TryGetValue(eventType, out controller);
            }
        }

        private sealed class FakeClock : IClock
        {
            public FakeClock(DateTimeOffset now)
            {
                UtcNow = now;
            }

            public DateTimeOffset UtcNow { get; }
        }

        private sealed class FakeStateStore : IStateStore
        {
            private readonly Dictionary<string, EventStateData> _loadedStates;

            public FakeStateStore(Dictionary<string, EventStateData> loadedStates)
            {
                _loadedStates = CloneStates(loadedStates);
                LastSavedStates = CloneStates(loadedStates);
            }

            public Dictionary<string, EventStateData> LastSavedStates { get; private set; }

            public UniTask<Dictionary<string, EventStateData>> LoadAsync(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(CloneStates(_loadedStates));
            }

            public UniTask SaveAsync(Dictionary<string, EventStateData> states, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                LastSavedStates = CloneStates(states);
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeTelemetry : IOrchestratorTelemetry
        {
            public int FailureCalls { get; private set; }
            public string LastFailureStage { get; private set; }

            public UniTask TrackTransitionAsync(string scheduleItemId, EventInstanceState from, EventInstanceState to, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public UniTask TrackStartRejectedAsync(string scheduleItemId, string reason, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public UniTask TrackFailureAsync(string scheduleItemId, string stage, Exception ex, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                FailureCalls++;
                LastFailureStage = stage;
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeEventController : IEventController
        {
            private readonly List<string> _callOrder;
            private readonly string _eventId;

            public FakeEventController(string eventType, List<string> callOrder)
            {
                EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
                _callOrder = callOrder ?? throw new ArgumentNullException(nameof(callOrder));
                _eventId = eventType == "CardCollection1" ? "event1" : "event2";
            }

            public string EventType { get; }

            public int InitializeCalls { get; private set; }
            public int OnStartCalls { get; private set; }
            public int OnUpdateCalls { get; private set; }
            public int OnEndCalls { get; private set; }
            public int ExecuteSettlementCalls { get; private set; }
            public Exception ThrowOnUpdate { get; set; }

            public UniTask InitializeAsync(ScheduleItem config, EventStateData state, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                InitializeCalls++;
                _callOrder.Add($"{_eventId}.Initialize");
                return UniTask.CompletedTask;
            }

            public UniTask OnStart(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                OnStartCalls++;
                _callOrder.Add($"{_eventId}.OnStart");
                return UniTask.CompletedTask;
            }

            public UniTask OnUpdate(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                OnUpdateCalls++;
                _callOrder.Add($"{_eventId}.OnUpdate");
                if (ThrowOnUpdate != null)
                {
                    throw ThrowOnUpdate;
                }

                return UniTask.CompletedTask;
            }

            public UniTask OnEnd(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                OnEndCalls++;
                _callOrder.Add($"{_eventId}.OnEnd");
                return UniTask.CompletedTask;
            }

            public UniTask ExecuteSettlement(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                ExecuteSettlementCalls++;
                _callOrder.Add($"{_eventId}.ExecuteSettlement");
                return UniTask.CompletedTask;
            }
        }

        private static Dictionary<string, EventStateData> CloneStates(Dictionary<string, EventStateData> source)
        {
            var clone = new Dictionary<string, EventStateData>(StringComparer.Ordinal);
            foreach (var pair in source)
            {
                var state = pair.Value;
                clone[pair.Key] = new EventStateData
                {
                    ScheduleItemId = state.ScheduleItemId,
                    State = state.State,
                    Version = state.Version,
                    UpdatedAtUtc = state.UpdatedAtUtc,
                    LastError = state.LastError,
                    StartInvoked = state.StartInvoked,
                    EndInvoked = state.EndInvoked,
                    SettlementInvoked = state.SettlementInvoked,
                };
            }

            return clone;
        }
    }
}
