using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Survival
{
    // Hybrid GameObject visual for the ECS player. Each LateUpdate:
    //   1. Copies the player entity's LocalTransform onto this GameObject.
    //   2. Feeds the input magnitude into the Animator (Speed parameter) so
    //      the Locomotion blend tree mixes Idle ↔ Run automatically.
    //   3. Consumes PlayerShotEvent entities and fires the Shoot trigger on
    //      the Animator so the bow-release anim plays in sync with each
    //      projectile spawn (mirrors the EnemyVisualPoolManager attack path).
    //
    // Place on a root GameObject that contains the imported character prefab
    // as a child (or directly on a prefab variant of Rogue_Hooded). Lives in
    // the regular scene, NOT in a SubScene.
    public sealed class PlayerVisualSync : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private string _shootTrigger = "Shoot";

        [Header("Tuning")]
        [Tooltip("Multiplier applied to MoveDirection.length before feeding into Animator.Speed. " +
                 "1 = raw input magnitude (0..1). Increase if your Animator's Run state expects >1.")]
        [SerializeField] private float _speedScale = 1f;

        [Tooltip("Visual offset relative to the ECS player position. Use Y to lift the model up " +
                 "if its pivot is off, or X/Z for slight forward-lean.")]
        [SerializeField] private Vector3 _positionOffset = Vector3.zero;

        [Tooltip("How fast the visual rotates toward the nearest enemy (when one is in range). " +
                 "Higher = snappier turn. ~12 feels responsive, ~4 feels lazy.")]
        [SerializeField] private float _aimRotationSpeed = 12f;

        private EntityQuery _playerQuery;
        private EntityQuery _shotEventsQuery;
        private bool _queriesReady;
        private int _speedHash;

        private void Awake()
        {
            if (!string.IsNullOrEmpty(_speedParam))
                _speedHash = Animator.StringToHash(_speedParam);
        }

        private void LateUpdate()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            if (!_queriesReady)
            {
                var em = world.EntityManager;
                _playerQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<PlayerTag>(),
                    ComponentType.ReadOnly<LocalTransform>(),
                    ComponentType.ReadOnly<MoveDirection>());
                _shotEventsQuery = em.CreateEntityQuery(typeof(PlayerShotEvent));
                _queriesReady = true;
            }

            SyncTransformAndSpeed(world);
            HandleShotEvents(world);
        }

        private void SyncTransformAndSpeed(World world)
        {
            if (_playerQuery.CalculateEntityCount() != 1)
                return;

            using var entities = _playerQuery.ToEntityArray(Allocator.Temp);
            var em = world.EntityManager;
            Entity player = entities[0];

            LocalTransform t = em.GetComponentData<LocalTransform>(player);
            MoveDirection dir = em.GetComponentData<MoveDirection>(player);

            // Position always comes from ECS.
            transform.position = (Vector3)t.Position + _positionOffset;

            // Rotation: if there's an enemy in range, face it (overrides
            // ECS-rotation which is movement-based). Otherwise fall back to
            // the ECS rotation so the visual still turns where you're running.
            quaternion targetRot = t.Rotation;
            if (em.HasComponent<AimDirection>(player))
            {
                AimDirection aim = em.GetComponentData<AimDirection>(player);
                if (math.lengthsq(aim.Value) > 1e-4f)
                    targetRot = quaternion.LookRotationSafe(aim.Value, math.up());
            }

            float lerp = math.saturate(_aimRotationSpeed * Time.deltaTime);
            transform.rotation = math.slerp(transform.rotation, targetRot, lerp);

            if (_animator != null && _speedHash != 0)
            {
                float magnitude = math.length(dir.Value);
                _animator.SetFloat(_speedHash, magnitude * _speedScale);
            }
        }

        private void HandleShotEvents(World world)
        {
            if (_shotEventsQuery.IsEmpty)
                return;

            using var events = _shotEventsQuery.ToEntityArray(Allocator.Temp);
            var em = world.EntityManager;

            if (_animator != null && !string.IsNullOrEmpty(_shootTrigger))
            {
                // One trigger per frame is enough — even if AutoShoot fires
                // multiple times (it shouldn't, but defensively), the Animator
                // would just queue another Shoot transition.
                _animator.SetTrigger(_shootTrigger);
            }

            em.DestroyEntity(events);
        }
    }
}
