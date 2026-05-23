using Unity.Entities;
using UnityEngine;

namespace Survival
{
    public class WeaponAuthoring : MonoBehaviour
    {
        public GameObject projectilePrefab;
        public float fireInterval = 0.5f;
        public float projectileSpeed = 40f;
        public float projectileDamage = 10f;
        public float projectileLifetime = 3f;

        public class Baker : Baker<WeaponAuthoring>
        {
            public override void Bake(WeaponAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new Weapon
                {
                    FireInterval = authoring.fireInterval,
                    FireTimer = 0f,
                    ProjectileSpeed = authoring.projectileSpeed,
                    ProjectileDamage = authoring.projectileDamage,
                    ProjectileLifetime = authoring.projectileLifetime,
                    ProjectilePrefab = authoring.projectilePrefab != null
                        ? GetEntity(authoring.projectilePrefab, TransformUsageFlags.Dynamic)
                        : Entity.Null
                });
            }
        }
    }
}
