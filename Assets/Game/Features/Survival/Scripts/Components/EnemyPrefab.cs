using Unity.Entities;

namespace Survival
{
    // Buffer of baked enemy prefab entities the spawner can instantiate.
    public struct EnemyPrefab : IBufferElementData
    {
        public Entity Value;
    }
}
