using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Survival
{
    // Hybrid GameObject pool for enemy visuals with support for multiple
    // visual prefab variants. Each ECS enemy entity gets one of the variants
    // deterministically (entity.Index % count) so that the same entity always
    // maps to the same visual type. Each prefab has its own sub-pool so that
    // returning a visual keeps the type stable.
    //
    // Per frame: syncs every active visual's transform with its entity's
    // LocalTransform, fires Attack animation when EnemyAttackEvent entities
    // are emitted by EnemyContactDamageJob, and on entity disappearance plays
    // the Death animation before recycling the visual back into its sub-pool.
    public sealed class EnemyVisualPoolManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] _visualPrefabs;
        [SerializeField] private Transform _parent;
        [SerializeField] private int _initialPoolSize = 32;
        [SerializeField] private float _deathAnimDuration = 1.5f;
        [SerializeField] private string _deathTrigger = "Death";
        [SerializeField] private string _attackTrigger = "Attack";

        private Queue<GameObject>[] _pools;
        private readonly Dictionary<Entity, ActiveVisual> _active = new();
        private readonly List<DyingVisual> _dying = new();
        private readonly HashSet<Entity> _aliveScratch = new();
        private readonly List<Entity> _removeScratch = new();

        private EntityQuery _enemyQuery;
        private EntityQuery _attackEventsQuery;
        private bool _queryReady;

        private struct ActiveVisual
        {
            public GameObject GameObject;
            public Animator Animator;
            public int PrefabIndex;
        }

        private struct DyingVisual
        {
            public GameObject GameObject;
            public float RemainingTime;
            public int PrefabIndex;
        }

        private void Start()
        {
            if (_visualPrefabs == null || _visualPrefabs.Length == 0)
            {
                Debug.LogWarning("[EnemyVisualPoolManager] _visualPrefabs is empty — no visuals will spawn.");
                return;
            }

            _pools = new Queue<GameObject>[_visualPrefabs.Length];
            int perPrefab = Mathf.Max(1, _initialPoolSize / _visualPrefabs.Length);
            for (int i = 0; i < _visualPrefabs.Length; i++)
            {
                _pools[i] = new Queue<GameObject>();
                if (_visualPrefabs[i] == null) continue;
                for (int j = 0; j < perPrefab; j++)
                    _pools[i].Enqueue(CreatePooled(i));
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                var em = world.EntityManager;
                _enemyQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<EnemyTag>(),
                    ComponentType.ReadOnly<LocalTransform>());
                _attackEventsQuery = em.CreateEntityQuery(typeof(EnemyAttackEvent));
                _queryReady = true;
            }
        }

        private GameObject CreatePooled(int prefabIndex)
        {
            Transform parent = _parent != null ? _parent : transform;
            var go = Instantiate(_visualPrefabs[prefabIndex], parent);
            go.SetActive(false);
            return go;
        }

        private void LateUpdate()
        {
            TickDying(Time.deltaTime);

            if (!_queryReady || _visualPrefabs == null || _visualPrefabs.Length == 0)
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            SyncTransforms();
            HandleAttackEvents(world);
        }

        private void SyncTransforms()
        {
            using var entities = _enemyQuery.ToEntityArray(Allocator.Temp);
            using var transforms = _enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            _aliveScratch.Clear();

            for (int i = 0; i < entities.Length; i++)
            {
                Entity e = entities[i];
                _aliveScratch.Add(e);

                if (!_active.TryGetValue(e, out ActiveVisual visual))
                {
                    int prefabIndex = Mathf.Abs(e.Index) % _visualPrefabs.Length;
                    GameObject go = _pools[prefabIndex].Count > 0
                        ? _pools[prefabIndex].Dequeue()
                        : CreatePooled(prefabIndex);
                    go.SetActive(true);
                    visual = new ActiveVisual
                    {
                        GameObject = go,
                        Animator = go.GetComponentInChildren<Animator>(),
                        PrefabIndex = prefabIndex
                    };
                    _active[e] = visual;
                }

                LocalTransform t = transforms[i];
                visual.GameObject.transform.SetPositionAndRotation(t.Position, t.Rotation);
            }

            // Visuals whose ECS entity no longer exists → trigger Death and
            // hand them to the dying list for recycling after the clip finishes.
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
                    RemainingTime = _deathAnimDuration,
                    PrefabIndex = kv.Value.PrefabIndex
                });
            }

            for (int i = 0; i < _removeScratch.Count; i++)
                _active.Remove(_removeScratch[i]);
        }

        private void HandleAttackEvents(World world)
        {
            if (_attackEventsQuery.IsEmpty)
                return;

            EntityManager em = world.EntityManager;

            using var events = _attackEventsQuery.ToComponentDataArray<EnemyAttackEvent>(Allocator.Temp);
            using var entities = _attackEventsQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < events.Length; i++)
            {
                if (!_active.TryGetValue(events[i].Attacker, out ActiveVisual visual))
                    continue;
                if (visual.Animator == null || string.IsNullOrEmpty(_attackTrigger))
                    continue;
                visual.Animator.SetTrigger(_attackTrigger);
            }

            em.DestroyEntity(entities);
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
                _pools[d.PrefabIndex].Enqueue(d.GameObject);
                _dying.RemoveAt(i);
            }
        }
    }
}
