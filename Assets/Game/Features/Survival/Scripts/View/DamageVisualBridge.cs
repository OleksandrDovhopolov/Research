using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UIShared;
using UnityEngine;
using VContainer;

namespace Survival
{
    // Consumes ECS DamageEvent entities each frame and forwards them to the
    // floating-numbers HUD widget. Destroys events after consuming so the
    // archetype clears.
    public sealed class DamageVisualBridge : MonoBehaviour
    {
        private IHudController _hudController;
        private DamageNumbersHudWidget _widget;
        private EntityQuery _eventsQuery;
        private CancellationTokenSource _cts;

        [Inject]
        private void Construct(IHudController hudController)
        {
            _hudController = hudController;
        }

        private void Start()
        {
            _cts = new CancellationTokenSource();

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            _eventsQuery = world.EntityManager.CreateEntityQuery(typeof(DamageEvent));

            InitWidgetAsync(_cts.Token).Forget();
        }

        private async UniTaskVoid InitWidgetAsync(CancellationToken ct)
        {
            if (_hudController == null) return;
            _widget = await _hudController.GetHudWidgetAsync<DamageNumbersHudWidget>(ct);
        }

        private void Update()
        {
            if (_widget == null) return;
            if (_eventsQuery.IsEmpty) return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            EntityManager em = world.EntityManager;

            using var events = _eventsQuery.ToComponentDataArray<DamageEvent>(Allocator.Temp);
            using var entities = _eventsQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < events.Length; i++)
            {
                var evt = events[i];
                _widget.Show(evt.Position, evt.Amount, evt.ToPlayer);
            }

            em.DestroyEntity(entities);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
