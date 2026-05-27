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

            spawn.ValueRW.Timer -= SystemAPI.Time.DeltaTime;
            if (spawn.ValueRW.Timer > 0f)
                return;

            // SpawnIntervalMultiplier < 1 → волны идут чаще на поздних стадиях.
            spawn.ValueRW.Timer += config.Interval * diff.SpawnIntervalMultiplier;

            var prefabs = SystemAPI.GetSingletonBuffer<EnemyPrefab>();
            if (prefabs.Length == 0)
                return;

            ArenaBounds bounds = SystemAPI.GetSingleton<ArenaBounds>();
            const float wallInset = 0.5f;  // не лепить ровно по стене

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            ref var rng = ref spawn.ValueRW.Random;
            int count = config.CountPerWave + diff.CountPerWaveAddend;
            for (int i = 0; i < count; i++)
            {
                Entity prefab = prefabs[rng.NextInt(prefabs.Length)].Value;

                // Случайная точка на rect-периметре арены (не на кольце вокруг
                // игрока). Сторона выбирается равновероятно, позиция вдоль
                // стороны — равномерно.
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

                // Read the prefab's baked stats — these are the "base" values
                // we scale by the current difficulty multipliers.
                Health baseHp = SystemAPI.GetComponent<Health>(prefab);
                ContactDamage baseCd = SystemAPI.GetComponent<ContactDamage>(prefab);

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
            }
        }
    }
}
