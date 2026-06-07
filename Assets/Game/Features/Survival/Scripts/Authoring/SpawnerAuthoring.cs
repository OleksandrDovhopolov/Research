using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Survival
{
    public class SpawnerAuthoring : MonoBehaviour
    {
        public GameObject[] enemyPrefabs;
        public float initialDelay = 3f;
        public float spawnInterval = 2f;
        public int countPerWave = 5;
        public float spawnRadius = 25f;
        public float moveSpeedMin = 0.7f;
        public float moveSpeedMax = 1.3f;
        [Tooltip("One-shot burst spawned at game start. 0 = disabled. " +
                 "Set to 300-500 for Burst-benchmarking / stress-test scenarios.")]
        public int initialBurstCount = 0;
        public uint seed = 1234;

        public class Baker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new SpawnConfig
                {
                    InitialDelay = authoring.initialDelay,
                    Interval = authoring.spawnInterval,
                    CountPerWave = authoring.countPerWave,
                    SpawnRadius = authoring.spawnRadius,
                    MoveSpeedMin = authoring.moveSpeedMin,
                    MoveSpeedMax = authoring.moveSpeedMax,
                    InitialBurstCount = authoring.initialBurstCount
                });

                AddComponent(entity, new SpawnState
                {
                    Timer = authoring.initialDelay,
                    Random = Random.CreateFromIndex(authoring.seed)
                });

                DynamicBuffer<EnemyPrefab> buffer = AddBuffer<EnemyPrefab>(entity);
                if (authoring.enemyPrefabs != null)
                {
                    foreach (var prefab in authoring.enemyPrefabs)
                    {
                        if (prefab == null)
                            continue;

                        buffer.Add(new EnemyPrefab
                        {
                            Value = GetEntity(prefab, TransformUsageFlags.Dynamic)
                        });
                    }
                }
            }
        }
    }
}
