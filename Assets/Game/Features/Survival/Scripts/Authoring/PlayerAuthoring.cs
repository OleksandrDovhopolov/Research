using Unity.Entities;
using UnityEngine;

namespace Survival
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float moveSpeed = 6f;
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
            }
        }
    }
}

