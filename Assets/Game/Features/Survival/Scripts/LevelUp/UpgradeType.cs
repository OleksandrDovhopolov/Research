namespace Survival
{
    // Extend with new values when adding new upgrade kinds — every new value
    // also needs a case in ApplyUpgradeSystem and a matching UpgradeDefinition SO.
    public enum UpgradeType : byte
    {
        FireRate = 0,
        Damage = 1,
        MaxHealth = 2,
        MultiShot = 3,
        BurstShot = 4,
    }
}
