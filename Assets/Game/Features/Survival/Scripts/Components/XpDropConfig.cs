using Unity.Entities;

namespace Survival
{
    // Singleton: where to find the XP gem prefab and how much XP each kill drops.
    public struct XpDropConfig : IComponentData
    {
        public Entity XpPrefab;
        public int XpPerKill;
        public float SpawnHeightOffset;
    }
}
