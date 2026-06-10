using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Survival
{
    public class EnemyAuthoring : MonoBehaviour
    {
        public float health = 30f;
        public float moveSpeed = 10f;
        public float rotationSpeed = 10f;

        [Header("Fake-run visual")]
        public float runSwayDegrees = 8f;
        public float runBobSpeed = 6f;
        public float runLeanDegrees = 15f;

        [Header("Contact damage")]
        public float contactDamagePerHit = 5f;
        public float contactInterval = 1f;
        public float contactRadius = 1.5f;

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

                // RunBob removed — real Animator-driven animation now lives on
                // a separate GameObject managed by EnemyVisualPoolManager.
                // EnemyRunBobSystem becomes dormant via RequireForUpdate<RunBob>.

                AddComponent(entity, new ContactDamage
                {
                    DamagePerHit = authoring.contactDamagePerHit,
                    Interval = authoring.contactInterval,
                    Timer = 0f,
                    Radius = authoring.contactRadius
                });
            }
        }
    }
}
