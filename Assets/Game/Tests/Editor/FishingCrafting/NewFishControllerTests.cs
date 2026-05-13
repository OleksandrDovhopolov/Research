using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Game.Fishing;
using NUnit.Framework;
using UISystem;
using UnityEngine;

namespace Game.Tests.Editor.FishingCrafting
{
    public sealed class NewFishControllerTests
    {
        private readonly List<UnityEngine.Object> _objectsToCleanup = new();

        [TearDown]
        public void TearDown()
        {
            for (var i = _objectsToCleanup.Count - 1; i >= 0; i--)
            {
                if (_objectsToCleanup[i] != null)
                    UnityEngine.Object.DestroyImmediate(_objectsToCleanup[i]);
            }

            _objectsToCleanup.Clear();
        }

        [Test]
        public void Hide_MarksViewed_WhenFishIsNew()
        {
            var fishBookService = new StubFishBookService();
            var controller = CreateController(fishBookService);
            var args = new NewFishArgs("perch", true, 2.75f, 3.5f, true, new[] { "common", "rare" });

            RunCoroutine(controller.Show(args));
            RunCoroutine(controller.Hide(true, 0f));

            Assert.That(fishBookService.MarkAsViewedCalls, Is.EqualTo(1));
            Assert.That(fishBookService.LastMarkedFishId, Is.EqualTo("perch"));
        }

        [Test]
        public void Hide_DoesNotMarkViewed_WhenFishIsNotNew()
        {
            var fishBookService = new StubFishBookService();
            var controller = CreateController(fishBookService);
            var args = new NewFishArgs("carp", false, 1.2f, 4.1f, true, new[] { "common", "epic" });

            RunCoroutine(controller.Show(args));
            RunCoroutine(controller.Hide(true, 0f));

            Assert.That(fishBookService.MarkAsViewedCalls, Is.EqualTo(0));
            Assert.That(fishBookService.LastMarkedFishId, Is.Null);
        }

        private NewFishController CreateController(StubFishBookService fishBookService)
        {
            var uiManagerGo = new GameObject("NewFishUIManager");
            var viewGo = new GameObject("NewFishView");

            _objectsToCleanup.Add(uiManagerGo);
            _objectsToCleanup.Add(viewGo);

            var uiManager = uiManagerGo.AddComponent<UIManager>();
            var view = viewGo.AddComponent<NewFishView>();

            var controller = new NewFishController();
            controller.Configurate(view, uiManager, new WindowAttribute("NewFishWindow", WindowType.Popup));
            controller.SetEventHandler(new StubEventHandler());

            var constructMethod = typeof(NewFishController).GetMethod("Construct", BindingFlags.Instance | BindingFlags.NonPublic);
            constructMethod.Invoke(controller, new object[] { fishBookService });

            return controller;
        }

        private static void RunCoroutine(IEnumerator enumerator)
        {
            while (enumerator.MoveNext())
            {
            }
        }

        private sealed class StubFishBookService : IFishBookService
        {
            public int MarkAsViewedCalls { get; private set; }
            public string LastMarkedFishId { get; private set; }

            public UniTask RegisterCatchAsync(FishingCatchResult result, System.Threading.CancellationToken ct = default)
            {
                return UniTask.CompletedTask;
            }

            public UniTask<FishBookProgress> GetProgressAsync(string fishId, System.Threading.CancellationToken ct = default)
            {
                return UniTask.FromResult<FishBookProgress>(null);
            }

            public UniTask MarkAsViewedAsync(string fishId, System.Threading.CancellationToken ct = default)
            {
                MarkAsViewedCalls++;
                LastMarkedFishId = fishId;
                return UniTask.CompletedTask;
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
