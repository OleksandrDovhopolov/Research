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

            foreach (var (upgrade, health, maxHealth, move, magnet, entity) in
                SystemAPI.Query<RefRO<PendingUpgrade>, RefRW<Health>, RefRW<MaxHealth>,
                                RefRW<MoveSpeed>, RefRW<XpMagnet>>()
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
                        // Raises BOTH the cap and current HP by u.Value. The
                        // clamp keeps current from overshooting max (shouldn't
                        // matter here since both grow together, but safe-by-design
                        // for future regen mixins).
                        maxHealth.ValueRW.Value += u.Value;
                        health.ValueRW.Value = math.min(
                            health.ValueRO.Value + u.Value,
                            maxHealth.ValueRO.Value);
                        break;
                    }
                    case UpgradeType.MultiShot:
                    {
                        var weapon = SystemAPI.GetSingletonRW<Weapon>();
                        weapon.ValueRW.ProjectileCount += (int)u.Value;
                        break;
                    }
                    case UpgradeType.BurstShot:
                    {
                        var weapon = SystemAPI.GetSingletonRW<Weapon>();
                        weapon.ValueRW.BurstCount += (int)u.Value;
                        break;
                    }
                    case UpgradeType.MoveSpeed:
                    {
                        // u.Value = 0.1 → +10% к скорости.
                        move.ValueRW.Value *= (1f + u.Value);
                        break;
                    }
                    case UpgradeType.MagnetRadius:
                    {
                        // u.Value = 0.25 → +25% к радиусу притяжения XP.
                        magnet.ValueRW.Radius *= (1f + u.Value);
                        break;
                    }
                }

                ecb.RemoveComponent<PendingUpgrade>(entity);
            }
        }
    }
}
