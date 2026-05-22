using Unity.Entities;
using UnityEngine;

namespace Survival
{
    public class EnemyAuthoring : MonoBehaviour
    {
        public float health = 30f;
        public float moveSpeed = 10f;
        public float rotationSpeed = 10f;

        public class Baker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new EnemyTag());

                AddComponent(entity, new Health
                {
                    Value = authoring.health
                });

                AddComponent(entity, new MoveSpeed
                {
                    Value = authoring.moveSpeed
                });

                AddComponent(entity, new RotationSpeed
                {
                    Value = authoring.rotationSpeed
                });
            }
        }
    }
}
