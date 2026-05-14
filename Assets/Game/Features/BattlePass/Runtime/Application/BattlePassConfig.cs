using System;

namespace BattlePass
{
    public static class BattlePassConfig
    {
        public static class Api
        {
        public const string CurrentPath = "battle-pass/current";
        public const string AddXpPath = "battle-pass/xp/add";
        public const string ClaimPath = "battle-pass/claim";
        public const string VerifyPurchasePath = "iap/google/verify";
        public const string DevGrantBattlePassPath = "battle-pass/dev/grant-battle-pass";
    }

        public static class Cache
        {
            public static readonly TimeSpan SnapshotTtl = TimeSpan.FromMinutes(3);
        }
    }
}
