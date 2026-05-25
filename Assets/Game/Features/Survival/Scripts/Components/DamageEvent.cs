using Unity.Entities;
using Unity.Mathematics;

namespace Survival
{
    // One-shot event entity emitted whenever damage is dealt.
    // DamageVisualBridge consumes (and destroys) these each frame to spawn
    // floating "-X" numbers in the HUD.
    public struct DamageEvent : IComponentData
    {
        public float3 Position;
        public float Amount;
        public bool ToPlayer; // colour differentiation: true = red, false = white
    }
}
