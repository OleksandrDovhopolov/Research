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

                AddComponent(entity, new PickupRadius
                {
                    Value = authoring.pickupRadius
                });

                AddComponent(entity, new MoveDirection
                {
                    Value = float3.zero
                });

                AddComponent(entity, new RotationSpeed
                {
                    Value = authoring.rotationSpeed
                });
            }
        }
    }
}

