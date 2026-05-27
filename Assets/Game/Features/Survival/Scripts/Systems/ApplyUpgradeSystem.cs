using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace Survival
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(LevelUpSystem))]
    [BurstCompile]
    public partial struct ApplyUpgradeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PendingUpgrade>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (upgrade, health, entity) in
                SystemAPI.Query<RefRO<PendingUpgrade>, RefRW<Health>>()
                    .WithAll<PlayerTag>()
                    .WithEntityAccess())
            {
                PendingUpgrade u = upgrade.ValueRO;

                switch (u.Type)
                {
                    case UpgradeType.FireRate:
                    {
                        var weapon = SystemAPI.GetSingletonRW<Weapon>();
                        weapon.ValueRW.FireInterval = math.max(
                            0.05f,
                            weapon.ValueRW.FireInterval * (1f - u.Value));
                        break;
                    }
                    case UpgradeType.Damage:
                    {
                        var weapon = SystemAPI.GetSingletonRW<Weapon>();
                        weapon.ValueRW.ProjectileDamage += u.Value;
                        break;
                    }
                    case UpgradeType.MaxHealth:
                    {
                        health.ValueRW.Value += u.Value;
                        break;
                    }
                    case UpgradeType.MultiShot:
                    {
                        var weapon = SystemAPI.GetSingletonRW<Weapon>();
                        weapon.ValueRW.ProjectileCount += (int)u.Value;
                        break;
                    }
                }

                ecb.RemoveComponent<PendingUpgrade>(entity);
            }
        }
    }
}
