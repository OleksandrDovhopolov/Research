using Unity.Entities;
using UnityEngine;

namespace Survival
{
    // Top-down follow camera for the Survivors mode. Disables the location
    // CameraBehaviour on the same GameObject and keeps the camera centered on
    // the player. Removing this component re-enables CameraBehaviour.
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] private float _followSpeed = 10f;

        private EntityQuery _query;
        private bool _hasQuery;
        private bool _initialized;

        private void Awake()
        {
            // Look up the location-camera controller by type name so the
            // Survival asmdef doesn't have to reference Assembly-CSharp where
            // CameraBehaviour lives. Equivalent to GetComponent<CameraBehaviour>().
            foreach (var behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour == null) continue;
                if (behaviour.GetType().Name != "CameraBehaviour") continue;
                behaviour.enabled = false;
                break;
            }
        }

        private void LateUpdate()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            if (!_hasQuery)
            {
                // PlayerPosition is written only by the main-thread PlayerMoveJob,
                // so reading it here never conflicts with parallel jobs.
                _query = world.EntityManager.CreateEntityQuery(typeof(PlayerPosition));
                _hasQuery = true;
            }

            if (_query.CalculateEntityCount() != 1)
                return;

            // Complete simulation jobs before reading ECS data from the main thread.
            world.EntityManager.CompleteAllTrackedJobs();
            var playerPos = _query.GetSingleton<PlayerPosition>().Value;
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
