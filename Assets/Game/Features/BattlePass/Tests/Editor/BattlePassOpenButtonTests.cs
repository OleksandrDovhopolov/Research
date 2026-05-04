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
using TMPro;
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
            var snapshotStore = new FakeSnapshotStore();

            var button = CreateButton(
                lifecycleState,
                snapshotStore,
                orchestrator,
                new FakeGlobalTimerService(),
                withTimerDisplay: true,
                out var buttonComponent,
                out _,
                out _,
                out _,
                out var timerDisplay);

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
            var snapshotStore = new FakeSnapshotStore();

            var button = CreateButton(
                lifecycleState,
                snapshotStore,
                orchestrator,
                new FakeGlobalTimerService(),
                withTimerDisplay: true,
                out var buttonComponent,
                out _,
                out _,
                out _,
                out var timerDisplay);

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
            var snapshotStore = new FakeSnapshotStore();

            var button = CreateButton(
                lifecycleState,
                snapshotStore,
                orchestrator,
                new FakeGlobalTimerService(),
                withTimerDisplay: true,
                out var buttonComponent,
                out _,
                out _,
                out _,
                out var timerDisplay);

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
            var snapshotStore = new FakeSnapshotStore();

            var button = CreateButton(
                lifecycleState,
                snapshotStore,
                orchestrator,
                new FakeGlobalTimerService(),
                withTimerDisplay: false,
                out _,
                out _,
                out _,
                out _,
                out _);

            Assert.DoesNotThrow(() =>
            {
                InvokeMethod(button, "Awake");
                InvokeMethod(button, "Start");
            });
        }

        [Test]
        public void PremiumClick_WhenPassNotOwned_OpensPurchaseWindowFlow()
        {
            var lifecycleState = new BattlePassLifecycleState();
            lifecycleState.SetStatus(BattlePassLifecycleStatus.Active);
            var snapshotStore = new FakeSnapshotStore
            {
                CurrentSnapshot = CreateSnapshot(BattlePassPassType.None, level: 1, xp: 120, seasonTitle: "Season Alpha")
            };

            var button = CreateButton(
                lifecycleState,
                snapshotStore,
                null,
                new FakeGlobalTimerService(),
                withTimerDisplay: false,
                out _,
                out var premiumButton,
                out _,
                out _,
                out _);

            InvokeMethod(button, "Awake");
            InvokeMethod(button, "Start");
            premiumButton.onClick.Invoke();

            Assert.That(button.ShowPurchaseWindowCalls, Is.EqualTo(1));
            Assert.That(button.LastPurchaseArgs, Is.Not.Null);
            Assert.That(button.LastPurchaseArgs.SeasonId, Is.EqualTo("season_1"));
            Assert.That(button.LastPurchaseArgs.ProductId, Is.EqualTo("premium_sku"));
            Assert.That(button.ShowBattlePassWindowCalls, Is.EqualTo(0));
            Assert.That(button.LastInfoMessage, Is.Null);
        }

        [Test]
        public void PremiumClick_WhenPassOwned_OpensBattlePassWindow()
        {
            var lifecycleState = new BattlePassLifecycleState();
            lifecycleState.SetStatus(BattlePassLifecycleStatus.Active);
            var snapshotStore = new FakeSnapshotStore
            {
                CurrentSnapshot = CreateSnapshot(BattlePassPassType.Premium, level: 1, xp: 120, seasonTitle: "Season Alpha")
            };

            var button = CreateButton(
                lifecycleState,
                snapshotStore,
                null,
                new FakeGlobalTimerService(),
                withTimerDisplay: false,
                out _,
                out var premiumButton,
                out _,
                out _,
                out _);

            InvokeMethod(button, "Awake");
            InvokeMethod(button, "Start");
            premiumButton.onClick.Invoke();

            Assert.That(button.ShowBattlePassWindowCalls, Is.EqualTo(1));
            Assert.That(button.ShowPurchaseWindowCalls, Is.EqualTo(0));
            Assert.That(button.LastPurchaseArgs, Is.Null);
            Assert.That(button.LastInfoMessage, Is.Null);
        }

        [Test]
        public void SnapshotRefresh_UpdatesSeasonTitle()
        {
            var lifecycleState = new BattlePassLifecycleState();
            lifecycleState.SetStatus(BattlePassLifecycleStatus.Active);
            var snapshotStore = new FakeSnapshotStore
            {
                CurrentSnapshot = CreateSnapshot(BattlePassPassType.None, level: 1, xp: 120, seasonTitle: "Season One")
            };

            var button = CreateButton(
                lifecycleState,
                snapshotStore,
                null,
                new FakeGlobalTimerService(),
                withTimerDisplay: false,
                out _,
                out _,
                out var seasonText,
                out _,
                out _);

            InvokeMethod(button, "Awake");
            InvokeMethod(button, "Start");
            Assert.That(seasonText.text, Is.EqualTo("Season One"));

            snapshotStore.ReplaceSnapshot(CreateSnapshot(BattlePassPassType.None, level: 1, xp: 120, seasonTitle: "Season Two"));
            Assert.That(seasonText.text, Is.EqualTo("Season Two"));
        }

        [Test]
        public void SnapshotRefresh_UpdatesXpSliderNormalizedValue()
        {
            var lifecycleState = new BattlePassLifecycleState();
            lifecycleState.SetStatus(BattlePassLifecycleStatus.Active);
            var snapshotStore = new FakeSnapshotStore
            {
                CurrentSnapshot = CreateSnapshot(BattlePassPassType.None, level: 1, xp: 150, seasonTitle: "Season One")
            };

            var button = CreateButton(
                lifecycleState,
                snapshotStore,
                null,
                new FakeGlobalTimerService(),
                withTimerDisplay: false,
                out _,
                out _,
                out _,
                out var xpSlider,
                out _);

            InvokeMethod(button, "Awake");
            InvokeMethod(button, "Start");

            Assert.That(xpSlider.value, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void SnapshotRefresh_WhenNoValidProgressData_ResetsSliderToZero()
        {
            var lifecycleState = new BattlePassLifecycleState();
            lifecycleState.SetStatus(BattlePassLifecycleStatus.Active);
            var snapshotStore = new FakeSnapshotStore
            {
                CurrentSnapshot = CreateInvalidProgressSnapshot()
            };

            var button = CreateButton(
                lifecycleState,
                snapshotStore,
                null,
                new FakeGlobalTimerService(),
                withTimerDisplay: false,
                out _,
                out _,
                out _,
                out var xpSlider,
                out _);

            InvokeMethod(button, "Awake");
            InvokeMethod(button, "Start");

            Assert.That(xpSlider.value, Is.EqualTo(0f).Within(0.0001f));
        }

        private TestBattlePassOpenButton CreateButton(
            BattlePassLifecycleState lifecycleState,
            IBattlePassSnapshotStore snapshotStore,
            EventOrchestrator orchestrator,
            IGlobalTimerService globalTimerService,
            bool withTimerDisplay,
            out Button unityButton,
            out Button premiumButton,
            out TMP_Text seasonText,
            out Slider xpSlider,
            out EventTimerDisplay timerDisplay)
        {
            var uiManagerGo = new GameObject("BattlePassButtonUIManager");
            var buttonGo = new GameObject("BattlePassOpenButton");
            GameObject timerGo = null;

            _objectsToCleanup.Add(uiManagerGo);
            _objectsToCleanup.Add(buttonGo);

            var uiManager = uiManagerGo.AddComponent<UIManager>();
            unityButton = buttonGo.AddComponent<Button>();
            premiumButton = new GameObject("PremiumButton").AddComponent<Button>();
            premiumButton.transform.SetParent(buttonGo.transform);
            seasonText = new GameObject("SeasonText").AddComponent<TextMeshProUGUI>();
            seasonText.transform.SetParent(buttonGo.transform);
            xpSlider = new GameObject("XpSlider", typeof(RectTransform)).AddComponent<Slider>();
            xpSlider.transform.SetParent(buttonGo.transform);
            var battlePassButton = buttonGo.AddComponent<TestBattlePassOpenButton>();

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
            SetField(battlePassButton, "_premiumButton", premiumButton);
            SetField(battlePassButton, "_seasonTitleText", seasonText);
            SetField(battlePassButton, "_xpSlider", xpSlider);
            SetField(battlePassButton, "_eventTimerDisplay", timerDisplay);

            var constructMethod = typeof(BattlePassOpenButton).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic);
            constructMethod.Invoke(battlePassButton, new object[] { uiManager, lifecycleState, snapshotStore, orchestrator, globalTimerService });

            return battlePassButton;
        }

        private static BattlePassSnapshot CreateSnapshot(BattlePassPassType passType, int level, int xp, string seasonTitle)
        {
            return new BattlePassSnapshot(
                new BattlePassSeason(
                    "season_1",
                    seasonTitle,
                    DateTimeOffset.Parse("2026-05-01T00:00:00Z"),
                    DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
                    50,
                    "active",
                    "v1"),
                new BattlePassProducts("premium_sku", "platinum_sku"),
                new BattlePassUserState(
                    "season_1",
                    level,
                    xp,
                    passType,
                    Array.Empty<BattlePassClaimedRewardCell>(),
                    Array.Empty<BattlePassClaimableRewardCell>()),
                new[]
                {
                    new BattlePassLevel(0, 0, null, null),
                    new BattlePassLevel(1, 100, null, null),
                    new BattlePassLevel(2, 200, null, null)
                },
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
        }

        private static BattlePassSnapshot CreateInvalidProgressSnapshot()
        {
            return new BattlePassSnapshot(
                new BattlePassSeason(
                    "season_1",
                    "Season Broken",
                    DateTimeOffset.Parse("2026-05-01T00:00:00Z"),
                    DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
                    50,
                    "active",
                    "v1"),
                new BattlePassProducts("premium_sku", "platinum_sku"),
                new BattlePassUserState(
                    "season_1",
                    3,
                    350,
                    BattlePassPassType.None,
                    Array.Empty<BattlePassClaimedRewardCell>(),
                    Array.Empty<BattlePassClaimableRewardCell>()),
                new[]
                {
                    new BattlePassLevel(1, 100, null, null)
                },
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
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

        private sealed class TestBattlePassOpenButton : BattlePassOpenButton
        {
            public int ShowBattlePassWindowCalls { get; private set; }
            public int ShowPurchaseWindowCalls { get; private set; }
            public string LastInfoMessage { get; private set; }
            public BattlePassIAPWindowArgs LastPurchaseArgs { get; private set; }

            protected override void ShowInfo(string message)
            {
                LastInfoMessage = message;
            }

            protected override void ShowBattlePassWindow()
            {
                ShowBattlePassWindowCalls++;
            }

            protected override void ShowPremiumPurchaseWindow(BattlePassIAPWindowArgs args)
            {
                ShowPurchaseWindowCalls++;
                LastPurchaseArgs = args;
            }
        }

        private sealed class FakeSnapshotStore : IBattlePassSnapshotStore
        {
            public event Action<BattlePassSnapshot> SnapshotChanged;

            public bool IsInitialized => true;
            public bool HasSnapshot => CurrentSnapshot != null;
            public bool LastSyncFailed => false;
            public BattlePassSnapshot CurrentSnapshot { get; set; }
            public DateTimeOffset LastSyncUtc => DateTimeOffset.UtcNow;
            public DateTimeOffset LastOpenRefreshUtc => DateTimeOffset.UtcNow;

            public bool IsStale(DateTimeOffset nowUtc)
            {
                return false;
            }

            public UniTask RefreshAsync(CancellationToken ct, bool force = false)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public void ReplaceSnapshot(BattlePassSnapshot snapshot)
            {
                CurrentSnapshot = snapshot;
                SnapshotChanged?.Invoke(snapshot);
            }

            public bool TryApplyUserState(BattlePassUserState updatedUserState)
            {
                if (updatedUserState == null || CurrentSnapshot == null)
                {
                    return false;
                }

                CurrentSnapshot = new BattlePassSnapshot(
                    CurrentSnapshot.Season,
                    CurrentSnapshot.Products,
                    updatedUserState,
                    CurrentSnapshot.Levels,
                    CurrentSnapshot.ServerTimeUtc);
                SnapshotChanged?.Invoke(CurrentSnapshot);
                return true;
            }
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
