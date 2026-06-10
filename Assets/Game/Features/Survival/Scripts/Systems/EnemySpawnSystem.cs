using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerPositionSystem))]
    [BurstCompile]
    public partial struct EnemySpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SpawnState>();
            state.RequireForUpdate<PlayerPosition>();
            state.RequireForUpdate<ArenaBounds>();
            state.RequireForUpdate<DifficultyState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<SpawnConfig>();
            var spawn = SystemAPI.GetSingletonRW<SpawnState>();
            DifficultyState diff = SystemAPI.GetSingleton<DifficultyState>();

            var prefabs = SystemAPI.GetSingletonBuffer<EnemyPrefab>();
            if (prefabs.Length == 0)
                return;

            ArenaBounds bounds = SystemAPI.GetSingleton<ArenaBounds>();

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // One-shot initial burst (stress-test / Burst-benchmark scenarios).
            // Runs once on the first tick after all dependencies are baked.
            if (!spawn.ValueRO.InitialBurstDone && config.InitialBurstCount > 0)
            {
                SpawnBatch(ref state, config.InitialBurstCount, ecb, prefabs, bounds,
                    ref spawn.ValueRW.Random, config, diff);
                spawn.ValueRW.InitialBurstDone = true;
            }

            spawn.ValueRW.Timer -= SystemAPI.Time.DeltaTime;
            if (spawn.ValueRW.Timer > 0f)
                return;

            // SpawnIntervalMultiplier < 1 → волны идут чаще на поздних стадиях.
            spawn.ValueRW.Timer += config.Interval * diff.SpawnIntervalMultiplier;

            int waveCount = config.CountPerWave + diff.CountPerWaveAddend;
            SpawnBatch(ref state, waveCount, ecb, prefabs, bounds,
                ref spawn.ValueRW.Random, config, diff);
        }

        // Спавнит N врагов одним вызовом. Используется и для обычной волны,
        // и для one-shot initial burst в начале матча.
        [BurstCompile]
        private void SpawnBatch(ref SystemState state, int count,
            EntityCommandBuffer ecb, DynamicBuffer<EnemyPrefab> prefabs,
            ArenaBounds bounds, ref Random rng,
            in SpawnConfig config, in DifficultyState diff)
        {
            const float wallInset = 0.5f;

            for (int i = 0; i < count; i++)
            {
                Entity prefab = prefabs[rng.NextInt(prefabs.Length)].Value;

                // Случайная точка на rect-периметре арены.
                int side = rng.NextInt(0, 4);
                float t = rng.NextFloat(0f, 1f);
                float2 p;
                switch (side)
                {
                    case 0: p = new float2(math.lerp(bounds.Min.x, bounds.Max.x, t), bounds.Max.y - wallInset); break;
                    case 1: p = new float2(bounds.Max.x - wallInset, math.lerp(bounds.Min.y, bounds.Max.y, t)); break;
                    case 2: p = new float2(math.lerp(bounds.Min.x, bounds.Max.x, t), bounds.Min.y + wallInset); break;
                    default: p = new float2(bounds.Min.x + wallInset, math.lerp(bounds.Min.y, bounds.Max.y, t)); break;
                }

                // Copy the prefab's baked LocalTransform so authored scale/rotation survive.
                LocalTransform transform = SystemAPI.GetComponent<LocalTransform>(prefab);
                transform.Position = new float3(p.x, 0f, p.y);

                // Базовые статы из bake'нутого префаба × множители difficulty + speed variance.
                Health baseHp = SystemAPI.GetComponent<Health>(prefab);
                ContactDamage baseCd = SystemAPI.GetComponent<ContactDamage>(prefab);
                MoveSpeed baseMove = SystemAPI.GetComponent<MoveSpeed>(prefab);

                float speedMult = rng.NextFloat(config.MoveSpeedMin, config.MoveSpeedMax);

                Entity enemy = ecb.Instantiate(prefab);
                ecb.SetComponent(enemy, transform);
                ecb.SetComponent(enemy, new Health
                {
                    Value = baseHp.Value * diff.HpMultiplier
                });
                ecb.SetComponent(enemy, new ContactDamage
                {
                    DamagePerHit = baseCd.DamagePerHit * diff.DamageMultiplier,
                    Interval = baseCd.Interval,
                    Timer = 0f,
                    Radius = baseCd.Radius
                });
                ecb.SetComponent(enemy, new MoveSpeed
                {
                    Value = baseMove.Value * speedMult
                });
            }
        }
    }
}
