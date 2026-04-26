using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using NUnit.Framework;
using Rewards;
using UISystem;
using UnityEngine;

namespace BattlePass.Tests.Editor
{
    public sealed class BattlePassWindowControllerTests
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
        public void Show_RendersBattlePassAndStartsTimer_WhenSeasonIsActive()
        {
            var snapshot = CreateActiveSnapshot();
            var serverService = new StubBattlePassServerService(snapshot);
            var timerService = new StubBattlePassTimerService();
            var controller = CreateController(serverService, timerService, out var view);

            RunCoroutine(controller.Show(null));

            Assert.That(view.RenderedModel, Is.Not.Null);
            Assert.That(view.UnavailableMessage, Is.Null.Or.Empty);
            Assert.That(timerService.StartCalls, Is.EqualTo(1));
            Assert.That(timerService.LastServerTimeUtc, Is.EqualTo(snapshot.ServerTimeUtc));
            Assert.That(timerService.LastEndAtUtc, Is.EqualTo(snapshot.Season.EndAtUtc));
        }

        [Test]
        public void Show_ShowsUnavailableState_WhenSeasonMissing()
        {
            var snapshot = new BattlePassSnapshot(
                null,
                null,
                null,
                Array.Empty<BattlePassLevel>(),
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
            var controller = CreateController(
                new StubBattlePassServerService(snapshot),
                new StubBattlePassTimerService(),
                out var view);

            RunCoroutine(controller.Show(null));

            Assert.That(view.RenderedModel, Is.Null);
            Assert.That(view.UnavailableMessage, Is.EqualTo(BattlePassConfig.Ui.UnavailableText));
        }

        [Test]
        public void Hide_StopsTimer_AndRemovesTimerSubscription()
        {
            var snapshot = CreateActiveSnapshot();
            var timerService = new StubBattlePassTimerService();
            var controller = CreateController(new StubBattlePassServerService(snapshot), timerService, out var view);

            RunCoroutine(controller.Show(null));
            var timerUpdateCountBeforeHide = view.TimerUpdateCount;

            RunCoroutine(controller.Hide(true, 0f));
            timerService.Emit(TimeSpan.FromSeconds(99));

            Assert.That(timerService.StopCalls, Is.EqualTo(1));
            Assert.That(view.TimerUpdateCount, Is.EqualTo(timerUpdateCountBeforeHide));
        }

        [Test]
        public void BattlePassLiveOpsController_OnStartAndOnEnd_UpdateLifecycleState()
        {
            var lifecycleState = new BattlePassLifecycleState();
            var controller = new BattlePassLiveOpsController(new BattlePassEventModelFactory(), lifecycleState);
            var schedule = new ScheduleItem
            {
                Id = "bp_1",
                EventType = "BattlePass",
                StreamId = "battle_pass",
                StartTimeUtc = DateTimeOffset.Parse("2026-05-01T00:00:00Z"),
                EndTimeUtc = DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
            };
            var state = new EventStateData
            {
                ScheduleItemId = "bp_1",
                State = EventInstanceState.Pending,
                UpdatedAtUtc = DateTimeOffset.Parse("2026-04-24T10:00:00Z"),
            };

            controller.InitializeAsync(schedule, state, CancellationToken.None).GetAwaiter().GetResult();
            controller.OnStart(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(lifecycleState.CurrentStatus, Is.EqualTo(BattlePassLifecycleStatus.Active));

            controller.OnEnd(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(lifecycleState.CurrentStatus, Is.EqualTo(BattlePassLifecycleStatus.Inactive));
        }

        private BattlePassWindowController CreateController(
            IBattlePassServerService serverService,
            IBattlePassTimerService timerService,
            out TestBattlePassView view)
        {
            var uiManagerGo = new GameObject("BattlePassUIManager");
            var viewGo = new GameObject("BattlePassView");

            _objectsToCleanup.Add(viewGo);
            _objectsToCleanup.Add(uiManagerGo);

            var uiManager = uiManagerGo.AddComponent<UIManager>();
            view = viewGo.AddComponent<TestBattlePassView>();

            var controller = new BattlePassWindowController();
            controller.Configurate(view, uiManager, new WindowAttribute("BattlePassWindow", WindowType.Popup));
            controller.SetEventHandler(new StubEventHandler());

            var rewardSpecProvider = new StubRewardSpecProvider(new Dictionary<string, RewardSpec>
            {
                ["reward_default"] = CreateRewardSpec("reward_default", 10),
                ["reward_premium"] = CreateRewardSpec("reward_premium", 25)
            });
            var factory = new BattlePassUiModelFactory(rewardSpecProvider);

            var constructMethod = typeof(BattlePassWindowController).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic);
            constructMethod.Invoke(controller, new object[] { serverService, timerService, factory });

            return controller;
        }

        private static BattlePassSnapshot CreateActiveSnapshot()
        {
            return new BattlePassSnapshot(
                new BattlePassSeason(
                    "season_1",
                    "Season 1",
                    DateTimeOffset.Parse("2026-05-01T00:00:00Z"),
                    DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
                    50,
                    "active",
                    "v1"),
                new BattlePassProducts("premium_sku", "platinum_sku"),
                new BattlePassUserState(
                    "season_1",
                    6,
                    180,
                    BattlePassPassType.Premium,
                    Array.Empty<BattlePassClaimedRewardCell>(),
                    Array.Empty<BattlePassClaimableRewardCell>()),
                new[]
                {
                    new BattlePassLevel(
                        1,
                        0,
                        new BattlePassRewardRef("reward_default"),
                        new BattlePassRewardRef("reward_premium"))
                },
                DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
        }

        private static RewardSpec CreateRewardSpec(string rewardId, int amount)
        {
            return new RewardSpec
            {
                RewardId = rewardId,
                Icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero),
                TotalAmountForUi = amount,
                Resources = new List<RewardSpecResource>()
            };
        }

        private static void RunCoroutine(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
            }
        }

        private sealed class TestBattlePassView : BattlePassView
        {
            public BattlePassWindowUiModel RenderedModel { get; private set; }
            public string UnavailableMessage { get; private set; }
            public int TimerUpdateCount { get; private set; }

            public override void ResetView()
            {
                RenderedModel = null;
                UnavailableMessage = null;
            }

            public override void Render(BattlePassWindowUiModel model)
            {
                RenderedModel = model;
                UnavailableMessage = null;
            }

            public override void ShowUnavailableState(string message)
            {
                UnavailableMessage = message;
                RenderedModel = null;
            }

            public override void SetTimer(TimeSpan remainingTime)
            {
                TimerUpdateCount++;
            }
        }

        private sealed class StubBattlePassServerService : IBattlePassServerService
        {
            private readonly BattlePassSnapshot _snapshot;

            public StubBattlePassServerService(BattlePassSnapshot snapshot)
            {
                _snapshot = snapshot;
            }

            public UniTask<BattlePassSnapshot> GetCurrentAsync(CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(_snapshot);
            }

            public UniTask<BattlePassUserState> AddXpAsync(int amount, CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                return UniTask.FromResult(_snapshot?.UserState);
            }
        }

        private sealed class StubBattlePassTimerService : IBattlePassTimerService
        {
            public event Action<TimeSpan> OnTimerUpdated;

            public int StartCalls { get; private set; }
            public int StopCalls { get; private set; }
            public DateTimeOffset LastServerTimeUtc { get; private set; }
            public DateTimeOffset LastEndAtUtc { get; private set; }
            public TimeSpan CurrentRemaining { get; private set; }

            public void Start(DateTimeOffset serverTimeUtc, DateTimeOffset endAtUtc)
            {
                StartCalls++;
                LastServerTimeUtc = serverTimeUtc;
                LastEndAtUtc = endAtUtc;
                CurrentRemaining = endAtUtc - serverTimeUtc;
                OnTimerUpdated?.Invoke(CurrentRemaining);
            }

            public void Stop()
            {
                StopCalls++;
                CurrentRemaining = TimeSpan.Zero;
            }

            public void UpdateNow()
            {
                OnTimerUpdated?.Invoke(CurrentRemaining);
            }

            public void Emit(TimeSpan remaining)
            {
                CurrentRemaining = remaining;
                OnTimerUpdated?.Invoke(remaining);
            }
        }

        private sealed class StubRewardSpecProvider : IRewardSpecProvider
        {
            private readonly Dictionary<string, RewardSpec> _specs;

            public StubRewardSpecProvider(Dictionary<string, RewardSpec> specs)
            {
                _specs = specs;
            }

            public bool TryGet(string rewardId, out RewardSpec spec)
            {
                return _specs.TryGetValue(rewardId, out spec);
            }
        }

        private sealed class StubEventHandler : UIManagerEventHandlerBase
        {
            public override void WindowShowEventInvoke(IWindowController window)
            {
            }

            public override void WindowHideEventInvoke(IWindowController window, bool isClosed)
            {
            }

            public override void WindowAnimationEventInvoke(IWindowController window, WindowAnimationType eventType)
            {
            }

            public override void StackCommandProcessedEventInvoke(UICommand uiCommand)
            {
            }

            public override void StackCommandProcessEventInvoke(UICommand uiCommand)
            {
            }

            public override void StackCommandProcessAddEventInvoke(UICommand uiCommand)
            {
            }
        }
    }
}
