using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Survival
{
    public class ArenaBoundsAuthoring : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(40f, 40f);

        public class Baker : Baker<ArenaBoundsAuthoring>
        {
            public override void Bake(ArenaBoundsAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);

                Vector3 center = authoring.transform.position;
                float halfX = authoring.size.x * 0.5f;
                float halfZ = authoring.size.y * 0.5f;

                AddComponent(entity, new ArenaBounds
                {
                    Min = new float2(center.x - halfX, center.z - halfZ),
                    Max = new float2(center.x + halfX, center.z + halfZ)
                });
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, 0.1f, size.y));
        }
    }
}
