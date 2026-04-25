using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;
using GameplayUI;
using NUnit.Framework;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassOpenButtonTests
    {
        private readonly List<UnityEngine.Object> _objectsToCleanup = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _objectsToCleanup)
            {
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }

            _objectsToCleanup.Clear();
        }

        [Test]
        public void Start_WhenBattlePassIsActive_BindsTimerToCurrentEvent()
        {
            var now = DateTimeOffset.Parse("2026-04-25T08:00:00Z");
            var schedule = new[]
            {
                new ScheduleItem
                {
                    Id = "bp_active",
                    EventType = BattlePassLiveOpsController.EventTypeValue,
                    StreamId = "battle_pass",
                    Priority = 1,
                    StartTimeUtc = now.AddHours(-1),
                    EndTimeUtc = now.AddHours(2),
                    CustomParams = new Dictionary<string, string>()
                }
            };
            var restoredStates = new Dictionary<string, EventStateData>
            {
                ["bp_active"] = new()
                {
                    ScheduleItemId = "bp_active",
                    State = EventInstanceState.Active,
                    Version = 1,
                    UpdatedAtUtc = now,
                    StartInvoked = true,
                }
            };

            var orchestrator = CreateOrchestrator(now, schedule, restoredStates);
            orchestrator.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            var lifecycleState = new BattlePassLifecycleState();
            lifecycleState.SetStatus(BattlePassLifecycleStatus.Active);

            var button = CreateButton(lifecycleState, orchestrator, new FakeGlobalTimerService(), true, out var buttonComponent, out var timerDisplay);

            InvokeMethod(button, "Awake");
            InvokeMethod(button, "Start");

            Assert.That(buttonComponent.interactable, Is.True);
            Assert.That(timerDisplay.gameObject.activeSelf, Is.True);
            Assert.That(GetEventTimerField<string>(timerDisplay, "_eventId"), Is.EqualTo("bp_active"));
            Assert.That(GetEventTimerField<IGlobalTimerService>(timerDisplay, "_globalTimerService"), Is.Not.Null);
        }

        [Test]
        public void Start_WhenBattlePassIsUpcoming_DoesNotBindTimer()
        {
            var now = DateTimeOffset.Parse("2026-04-25T08:00:00Z");
            var schedule = new[]
            {
                new ScheduleItem
                {
                    Id = "bp_upcoming",
                    EventType = BattlePassLiveOpsController.EventTypeValue,
                    StreamId = "battle_pass",
                    Priority = 1,
                    StartTimeUtc = now.AddHours(1),
                    EndTimeUtc = now.AddHours(3),
                    CustomParams = new Dictionary<string, string>()
                }
            };
            var restoredStates = new Dictionary<string, EventStateData>
            {
                ["bp_upcoming"] = new()
                {
                    ScheduleItemId = "bp_upcoming",
                    State = EventInstanceState.Pending,
                    Version = 1,
                    UpdatedAtUtc = now,
                }
            };

            var orchestrator = CreateOrchestrator(now, schedule, restoredStates);
            orchestrator.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            var lifecycleState = new BattlePassLifecycleState();
            lifecycleState.SetStatus(BattlePassLifecycleStatus.Upcoming);

            var button = CreateButton(lifecycleState, orchestrator, new FakeGlobalTimerService(), true, out var buttonComponent, out var timerDisplay);

            InvokeMethod(button, "Awake");
            InvokeMethod(button, "Start");

            Assert.That(buttonComponent.interactable, Is.True);
            Assert.That(timerDisplay.gameObject.activeSelf, Is.False);
            Assert.That(GetEventTimerField<IGlobalTimerService>(timerDisplay, "_globalTimerService"), Is.Null);
        }

        [Test]
        public void RefreshView_WhenLifecycleBecomesInactive_UnbindsAndHidesTimer()
        {
            var now = DateTimeOffset.Parse("2026-04-25T08:00:00Z");
            var schedule = new[]
            {
                new ScheduleItem
                {
                    Id = "bp_active",
                    EventType = BattlePassLiveOpsController.EventTypeValue,
                    StreamId = "battle_pass",
                    Priority = 1,
                    StartTimeUtc = now.AddHours(-1),
                    EndTimeUtc = now.AddHours(2),
                    CustomParams = new Dictionary<string, string>()
                }
            };
            var restoredStates = new Dictionary<string, EventStateData>
            {
                ["bp_active"] = new()
                {
                    ScheduleItemId = "bp_active",
                    State = EventInstanceState.Active,
                    Version = 1,
                    UpdatedAtUtc = now,
                    StartInvoked = true,
                }
            };

            var orchestrator = CreateOrchestrator(now, schedule, restoredStates);
            orchestrator.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            var lifecycleState = new BattlePassLifecycleState();
            lifecycleState.SetStatus(BattlePassLifecycleStatus.Active);

            var button = CreateButton(lifecycleState, orchestrator, new FakeGlobalTimerService(), true, out var buttonComponent, out var timerDisplay);

            InvokeMethod(button, "Awake");
            InvokeMethod(button, "Start");
            lifecycleState.SetStatus(BattlePassLifecycleStatus.Inactive);

            Assert.That(buttonComponent.interactable, Is.False);
            Assert.That(timerDisplay.gameObject.activeSelf, Is.False);
            Assert.That(GetEventTimerField<IGlobalTimerService>(timerDisplay, "_globalTimerService"), Is.Null);
        }

        [Test]
        public void Start_WhenTimerDisplayIsMissing_DoesNotThrow()
        {
            var now = DateTimeOffset.Parse("2026-04-25T08:00:00Z");
            var schedule = new[]
            {
                new ScheduleItem
                {
                    Id = "bp_active",
                    EventType = BattlePassLiveOpsController.EventTypeValue,
                    StreamId = "battle_pass",
                    Priority = 1,
                    StartTimeUtc = now.AddHours(-1),
                    EndTimeUtc = now.AddHours(2),
                    CustomParams = new Dictionary<string, string>()
                }
            };
            var restoredStates = new Dictionary<string, EventStateData>
            {
                ["bp_active"] = new()
                {
                    ScheduleItemId = "bp_active",
                    State = EventInstanceState.Active,
                    Version = 1,
                    UpdatedAtUtc = now,
                    StartInvoked = true,
                }
            };

            var orchestrator = CreateOrchestrator(now, schedule, restoredStates);
            orchestrator.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            var lifecycleState = new BattlePassLifecycleState();
            lifecycleState.SetStatus(BattlePassLifecycleStatus.Active);

            var button = CreateButton(lifecycleState, orchestrator, new FakeGlobalTimerService(), false, out _, out _);

            Assert.DoesNotThrow(() =>
            {
                InvokeMethod(button, "Awake");
                InvokeMethod(button, "Start");
            });
        }

        private BattlePassOpenButton CreateButton(
            BattlePassLifecycleState lifecycleState,
            EventOrchestrator orchestrator,
            IGlobalTimerService globalTimerService,
            bool withTimerDisplay,
            out Button unityButton,
            out EventTimerDisplay timerDisplay)
        {
            var uiManagerGo = new GameObject("BattlePassButtonUIManager");
            var buttonGo = new GameObject("BattlePassOpenButton");
            GameObject timerGo = null;

            _objectsToCleanup.Add(uiManagerGo);
            _objectsToCleanup.Add(buttonGo);

            var uiManager = uiManagerGo.AddComponent<UIManager>();
            unityButton = buttonGo.AddComponent<Button>();
            var battlePassButton = buttonGo.AddComponent<BattlePassOpenButton>();

            timerDisplay = null;
            if (withTimerDisplay)
            {
                timerGo = new GameObject("BattlePassTimerDisplay");
                timerGo.SetActive(false);
                timerGo.transform.SetParent(buttonGo.transform);
                timerDisplay = timerGo.AddComponent<EventTimerDisplay>();
                _objectsToCleanup.Add(timerGo);
            }

            SetField(battlePassButton, "_button", unityButton);
            SetField(battlePassButton, "_eventTimerDisplay", timerDisplay);

            var constructMethod = typeof(BattlePassOpenButton).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic);
            constructMethod.Invoke(battlePassButton, new object[] { uiManager, lifecycleState, orchestrator, globalTimerService });

            return battlePassButton;
        }

        private static EventOrchestrator CreateOrchestrator(
            DateTimeOffset now,
            IReadOnlyList<ScheduleItem> schedule,
            Dictionary<string, EventStateData> restoredStates)
        {
            var clock = new FakeClock(now);
            var stateStore = new FakeStateStore(restoredStates);
            var telemetry = new FakeTelemetry();
            var registry = new EmptyEventRegistry();
            var engine = new EventLifecycleEngine(registry, clock, stateStore, telemetry);

            return new EventOrchestrator(
                new StaticScheduleProvider(schedule),
                new BasicScheduleValidator(),
                clock,
                stateStore,
                engine);
        }

        private static T GetEventTimerField<T>(EventTimerDisplay timerDisplay, string fieldName)
        {
            var field = typeof(EventTimerDisplay).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Field '{fieldName}' was not found.");
            return (T)field.GetValue(timerDisplay);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field, $"Field '{fieldName}' was not found.");
            field.SetValue(target, value);
        }

        private static void InvokeMethod(object target, string methodName)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method, $"Method '{methodName}' was not found.");
            method.Invoke(target, null);
        }

        private sealed class StaticScheduleProvider : IScheduleProvider
        {
            private readonly IReadOnlyList<ScheduleItem> _schedule;

            public StaticScheduleProvider(IReadOnlyList<ScheduleItem> schedule)
            {
                _schedule = schedule;
            }

            public UniTask<IReadOnlyList<ScheduleItem>> LoadAsync(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(_schedule);
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
                _loadedStates = loadedStates;
            }

            public UniTask<Dictionary<string, EventStateData>> LoadAsync(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(_loadedStates);
            }

            public UniTask SaveAsync(Dictionary<string, EventStateData> states, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeTelemetry : IOrchestratorTelemetry
        {
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
                return UniTask.CompletedTask;
            }
        }

        private sealed class EmptyEventRegistry : IEventRegistry
        {
            public void Register(IEventController controller)
            {
            }

            public bool TryGet(string eventType, out IEventController controller)
            {
                controller = null;
                return false;
            }
        }

        private sealed class FakeGlobalTimerService : IGlobalTimerService
        {
            public event Action<string, TimeSpan> OnTick;
            public event Action<string> OnTimerFinished;

            public void Register(string eventId, DateTimeOffset endTimeUtc)
            {
            }

            public void Unregister(string eventId)
            {
            }

            public bool TryGetRemaining(string eventId, out TimeSpan remaining)
            {
                remaining = TimeSpan.FromMinutes(10);
                return true;
            }
        }
    }
}
