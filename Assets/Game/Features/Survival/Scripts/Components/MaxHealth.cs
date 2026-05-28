using Unity.Entities;

namespace Survival
{
    // Maximum health cap. Health.Value is the current; MaxHealth.Value is the
    // ceiling. The MaxHealth upgrade raises this, regen (future) tops up to it,
    // and PlayerHealthHudWidget reads ratio = Health / MaxHealth.
    public struct MaxHealth : IComponentData
    {
        public float Value;
    }
}
