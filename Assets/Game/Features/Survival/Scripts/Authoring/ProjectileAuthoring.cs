using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Survival
{
    public class ProjectileAuthoring : MonoBehaviour
    {
        public float hitRadius = 3f;

        public class Baker : Baker<ProjectileAuthoring>
        {
            public override void Bake(ProjectileAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new ProjectileTag());
                AddComponent(entity, new HitRadius { Value = authoring.hitRadius });

                // Placeholders — AutoShootSystem sets the real values on spawn.
                AddComponent(entity, new Damage { Value = 0f });
                AddComponent(entity, new MoveSpeed { Value = 0f });
                AddComponent(entity, new MoveDirection { Value = float3.zero });
                AddComponent(entity, new Lifetime { Value = 0f });
            }
        }
    }
}
