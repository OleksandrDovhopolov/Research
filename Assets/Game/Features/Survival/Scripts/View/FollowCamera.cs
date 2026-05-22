using CameraModule;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Survival
{
    // Top-down follow camera for the Survivors mode. Disables the location
    // CameraBehaviour on the same GameObject and keeps the camera centered on
    // the player ECS entity. Removing this component re-enables CameraBehaviour.
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] private float _followSpeed = 10f;

        private EntityQuery _query;
        private bool _hasQuery;
        private bool _initialized;

        private void Awake()
        {
            var cameraBehaviour = GetComponent<CameraBehaviour>();
            if (cameraBehaviour != null)
                cameraBehaviour.enabled = false;
        }

        private void LateUpdate()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            if (!_hasQuery)
            {
                _query = world.EntityManager.CreateEntityQuery(
                    typeof(PlayerTag), typeof(LocalTransform));
                _hasQuery = true;
            }

            if (_query.CalculateEntityCount() != 1)
                return;

            var playerPos = _query.GetSingleton<LocalTransform>().Position;
            var current = transform.position;
            var target = new Vector3(playerPos.x, current.y, playerPos.z);

            if (!_initialized)
            {
                transform.position = target;
                _initialized = true;
            }
            else
            {
                transform.position = Vector3.Lerp(current, target,
                    1f - Mathf.Exp(-_followSpeed * Time.deltaTime));
            }
        }
    }
}
