using Unity.Entities;

namespace Survival
{
    // Pending burst-shot queue. AutoShootSystem fires the main volley, then
    // ticks this until RemainingShots == 0, firing a follow-up volley each
    // time NextShotTimer hits zero. BurstCount on Weapon controls how many
    // total volleys per fire-interval (1 = no burst, 2 = double-shot, etc).
    public struct WeaponBurstState : IComponentData
    {
        public int RemainingShots;
        public float NextShotTimer;
    }
}
