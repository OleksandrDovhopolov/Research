namespace FortuneWheel
{
    public static class FortuneWheelConfig
    {
        public static class Gameplay
        {
            public const int SectorCount = 8;
            public const string AdSpinRewardId = "fortune_wheel_ad_spin";
        }

        public static class Api
        {
            public const string DataPath = "wheel/data";
            public const string RewardsPath = "wheel/rewards";
            public const string SpinPath = "wheel/spin";
            public const long FallbackNextUpdateAt = 0L;
            public const bool FallbackAdSpinAvailable = false;
        }

        public static class Animation
        {
            public const float PointerAngle = 0f;
            public const float FullCircle = 360f;
            public const float MinSpinDuration = 0.01f;
        }
    }
}
