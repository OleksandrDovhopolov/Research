using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Survival
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float moveSpeed = 6f;
        public float rotationSpeed = 12f;
        public float health = 100f;
        public float pickupRadius = 2f;
        public float xpMagnetRadius = 8f;
        public float xpMagnetSpeed = 25f;
        public int firstLevelUpXp = 8;

        public class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new PlayerTag());

                AddComponent(entity, new MoveSpeed
                {
                    Value = authoring.moveSpeed
                });

                AddComponent(entity, new Health
                {
                    Value = authoring.health
                });

                AddComponent(entity, new MaxHealth
                {
                    Value = authoring.health
                });

                AddComponent(entity, new PickupRadius
                {
                    Value = authoring.pickupRadius
                });

                AddComponent(entity, new XpMagnet
                {
                    Radius = authoring.xpMagnetRadius,
                    Speed = authoring.xpMagnetSpeed
                });

                AddComponent(entity, new MoveDirection
                {
                    Value = float3.zero
                });

                AddComponent(entity, new RotationSpeed
                {
                    Value = authoring.rotationSpeed
                });

                AddComponent(entity, new PlayerPosition
                {
                    Value = float3.zero
                });

                AddComponent(entity, new AimDirection
                {
                    Value = float3.zero
                });

                AddComponent(entity, new Experience
                {
                    Current = 0
                });

                AddComponent(entity, new Level
                {
                    Value = 1,
                    NextThresholdXp = authoring.firstLevelUpXp
                });
            }
        }
    }
}

