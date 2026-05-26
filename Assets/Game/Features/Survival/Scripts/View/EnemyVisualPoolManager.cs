using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Survival
{
    // Hybrid GameObject pool for enemy visuals. The ECS enemy entity holds only
    // gameplay components (EnemyTag, Health, MoveSpeed, ContactDamage). The
    // real visual — SkinnedMeshRenderer + Animator + AnimationController from
    // the zombie pack — lives on a separate GameObject pulled from this pool.
    //
    // Each frame the manager syncs every active visual's transform with its
    // ECS entity's LocalTransform. When the ECS entity disappears (killed),
    // the manager triggers the Death animation on the visual, then recycles it
    // back to the pool after _deathAnimDuration.
    public sealed class EnemyVisualPoolManager : MonoBehaviour
    {
        [SerializeField] private GameObject _visualPrefab;
        [SerializeField] private Transform _parent;
        [SerializeField] private int _initialPoolSize = 32;
        [SerializeField] private float _deathAnimDuration = 1.5f;
        [SerializeField] private string _deathTrigger = "Death";

        private readonly Queue<GameObject> _pool = new();
        private readonly Dictionary<Entity, ActiveVisual> _active = new();
        private readonly List<DyingVisual> _dying = new();
        private readonly HashSet<Entity> _aliveScratch = new();
        private readonly List<Entity> _removeScratch = new();

        private EntityQuery _enemyQuery;
        private bool _queryReady;

        private struct ActiveVisual
        {
            public GameObject GameObject;
            public Animator Animator;
        }

        private struct DyingVisual
        {
            public GameObject GameObject;
            public float RemainingTime;
        }

        private void Start()
        {
            if (_visualPrefab == null)
            {
                Debug.LogWarning("[EnemyVisualPoolManager] _visualPrefab is not assigned — no visuals will spawn.");
                return;
            }

            for (int i = 0; i < _initialPoolSize; i++)
                _pool.Enqueue(CreatePooled());

            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _enemyQuery = world.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<EnemyTag>(),
                    ComponentType.ReadOnly<LocalTransform>());
                _queryReady = true;
            }
        }

        private GameObject CreatePooled()
        {
            Transform parent = _parent != null ? _parent : transform;
            var go = Instantiate(_visualPrefab, parent);
            go.SetActive(false);
            return go;
        }

        private void LateUpdate()
        {
            TickDying(Time.deltaTime);

            if (!_queryReady || _visualPrefab == null)
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            using var entities = _enemyQuery.ToEntityArray(Allocator.Temp);
            using var transforms = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            _aliveScratch.Clear();

            for (int i = 0; i < entities.Length; i++)
            {
                Entity e = entities[i];
                _aliveScratch.Add(e);

                if (!_active.TryGetValue(e, out ActiveVisual visual))
                {
                    GameObject go = _pool.Count > 0 ? _pool.Dequeue() : CreatePooled();
                    go.SetActive(true);
                    visual = new ActiveVisual
                    {
                        GameObject = go,
                        Animator = go.GetComponentInChildren<Animator>()
                    };
                    _active[e] = visual;
                }

                LocalTransform t = transforms[i];
                visual.GameObject.transform.SetPositionAndRotation(t.Position, t.Rotation);
            }

            // Visuals whose ECS entity no longer exists → trigger Death anim and
            // move them to the dying list for recycling after the clip finishes.
            _removeScratch.Clear();
            foreach (var kv in _active)
            {
                if (_aliveScratch.Contains(kv.Key))
                    continue;

                _removeScratch.Add(kv.Key);

                if (kv.Value.Animator != null && !string.IsNullOrEmpty(_deathTrigger))
                    kv.Value.Animator.SetTrigger(_deathTrigger);

                _dying.Add(new DyingVisual
                {
                    GameObject = kv.Value.GameObject,
                    RemainingTime = _deathAnimDuration
                });
            }

            for (int i = 0; i < _removeScratch.Count; i++)
                _active.Remove(_removeScratch[i]);
        }

        private void TickDying(float dt)
        {
            for (int i = _dying.Count - 1; i >= 0; i--)
            {
                DyingVisual d = _dying[i];
                d.RemainingTime -= dt;
                if (d.RemainingTime > 0f)
                {
                    _dying[i] = d;
                    continue;
                }

                d.GameObject.SetActive(false);
                _pool.Enqueue(d.GameObject);
                _dying.RemoveAt(i);
            }
        }
    }
}
