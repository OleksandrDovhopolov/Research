using Unity.Entities;
using UnityEngine;

namespace Survival
{
    public class XpDropAuthoring : MonoBehaviour
    {
        public GameObject xpPrefab;
        public int xpPerKill = 1;
        public float spawnHeightOffset = 0.5f;

        public class Baker : Baker<XpDropAuthoring>
        {
            public override void Bake(XpDropAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new XpDropConfig
                {
                    XpPrefab = authoring.xpPrefab != null
                        ? GetEntity(authoring.xpPrefab, TransformUsageFlags.Dynamic)
                        : Entity.Null,
                    XpPerKill = authoring.xpPerKill,
                    SpawnHeightOffset = authoring.spawnHeightOffset
                });
            }
        }
    }
}
