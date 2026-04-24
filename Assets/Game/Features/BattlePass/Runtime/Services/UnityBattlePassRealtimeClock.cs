using UnityEngine;

namespace BattlePass
{
    public sealed class UnityBattlePassRealtimeClock : IBattlePassRealtimeClock
    {
        public double RealtimeSinceStartup => Time.realtimeSinceStartup;
    }
}
