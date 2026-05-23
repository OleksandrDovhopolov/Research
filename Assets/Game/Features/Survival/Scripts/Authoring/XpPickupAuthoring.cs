using Unity.Entities;
using UnityEngine;

namespace Survival
{
    public class XpPickupAuthoring : MonoBehaviour
    {
        public class Baker : Baker<XpPickupAuthoring>
        {
            public override void Bake(XpPickupAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new XpPickupTag());

                // Placeholder — DamageJob sets the real value at spawn.
                AddComponent(entity, new XpValue { Value = 0 });
            }
        }
    }
}
