using UnityEngine;

namespace Game.Fishing
{
    public sealed class FishingHudLureRenderData
    {
        public FishingHudLureRenderData(FishingHudLureViewData lure, Sprite sprite)
        {
            Lure = lure;
            Sprite = sprite;
        }

        public FishingHudLureViewData Lure { get; }
        public Sprite Sprite { get; }
    }
}
