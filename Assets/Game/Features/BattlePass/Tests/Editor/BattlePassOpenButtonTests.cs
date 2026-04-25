using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration;
using EventOrchestration.Abstractions;
using EventOrchestration.Models;
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
        public void Start_WhenLifecycleBecomesActive_ShowsActiveBadge()
        {
            var lifecycleState = new BattlePassLifecycleState();
            var button = CreateButton(lifecycleState, null, out var buttonComponent, out var upcomingBadge, out var activeBadge);

            InvokeMethod(button, "Awake");
            InvokeMethod(button, "Start");
            lifecycleState.SetStatus(BattlePassLifecycleStatus.Active);

            Assert.That(activeBadge.activeSelf, Is.True);
            Assert.That(upcomingBadge.activeSelf, Is.False);
            Assert.That(buttonComponent.interactable, Is.True);
        }

        [Test]
        public void RefreshScheduleAsync_WhenUpcomingBattlePassAppears_ShowsUpcomingBadge()
        {
            var now = DateTimeOffset.Parse("2026-04-25T08:00:00Z");
            var scheduleProvider = new MutableScheduleProvider(Array.Empty<ScheduleItem>());
            var orchestrator = CreateOrchestrator(now, scheduleProvider);
            orchestrator.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();

            var lifecycleState = new BattlePassLifecycleState();
            var button = CreateButton(lifecycleState, orchestrator, out _, out var upcomingBadge, out var activeBadge);

            InvokeMethod(button, "Awake");
            InvokeMethod(button, "Start");
            Assert.That(upcomingBadge.activeSelf, Is.False);

            scheduleProvider.Schedule = new[]
            {
                new ScheduleItem
                {
                    Id = "bp_upcoming",
                    EventType = BattlePassLiveOpsController.EventTypeValue,
                    StreamId = "battle_pass",
                    Priority = 1,
                    StartTimeUtc = now.AddHours(1),
                    EndTimeUtc = now.AddHours(2),
                    CustomParams = new Dictionary<string, string>()
                }
            };

            orchestrator.RefreshScheduleAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(upcomingBadge.activeSelf, Is.True);
            Assert.That(activeBadge.activeSelf, Is.False);
        }

        private BattlePassOpenButton CreateButton(
            BattlePassLifecycleState lifecycleState,
            EventOrchestrator orchestrator,
            out Button unityButton,
            out GameObject upcomingBadge,
            out GameObject activeBadge)
        {
            var uiManagerGo = new GameObject("BattlePassButtonUIManager");
            var buttonGo = new GameObject("BattlePassOpenButton");
            var upcomingBadgeGo = new GameObject("UpcomingBadge");
            var activeBadgeGo = new GameObject("ActiveBadge");

            upcomingBadgeGo.transform.SetParent(buttonGo.transform);
            activeBadgeGo.transform.SetParent(buttonGo.transform);

            _objectsToCleanup.Add(uiManagerGo);
            _objectsToCleanup.Add(buttonGo);

            var uiManager = uiManagerGo.AddComponent<UIManager>();
            unityButton = buttonGo.AddComponent<Button>();
            var battlePassButton = buttonGo.AddComponent<BattlePassOpenButton>();

            upcomingBadge = upcomingBadgeGo;
            activeBadge = activeBadgeGo;

            SetField(battlePassButton, "_button", unityButton);
            SetField(battlePassButton, "_upcomingBadgeRoot", upcomingBadgeGo);
            SetField(battlePassButton, "_activeBadgeRoot", activeBadgeGo);
            SetField(battlePassButton, "_allowClickWhenInactive", true);

            var constructMethod = typeof(BattlePassOpenButton).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic);
            constructMethod.Invoke(battlePassButton, new object[] { uiManager, lifecycleState, orchestrator });

            return battlePassButton;
        }

        private static EventOrchestrator CreateOrchestrator(DateTimeOffset now, MutableScheduleProvider scheduleProvider)
        {
            var clock = new FakeClock(now);
            var stateStore = new FakeStateStore();
            var telemetry = new FakeTelemetry();
            var registry = new EmptyEventRegistry();
            var engine = new EventLifecycleEngine(registry, clock, stateStore, telemetry);

            return new EventOrchestrator(
                scheduleProvider,
                new BasicScheduleValidator(),
                clock,
                stateStore,
                engine);
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

        private sealed class MutableScheduleProvider : IScheduleProvider
        {
            public MutableScheduleProvider(IReadOnlyList<ScheduleItem> schedule)
            {
                Schedule = schedule;
            }

            public IReadOnlyList<ScheduleItem> Schedule { get; set; }

            public UniTask<IReadOnlyList<ScheduleItem>> LoadAsync(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(Schedule);
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
            public UniTask<Dictionary<string, EventStateData>> LoadAsync(CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(new Dictionary<string, EventStateData>());
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
    }
}
