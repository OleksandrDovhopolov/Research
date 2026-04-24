using VContainer;

namespace BattlePass
{
    public static class BattlePassInstaller
    {
        public static void RegisterBattlePass(this IContainerBuilder builder)
        {
            builder.Register<IBattlePassRealtimeClock, UnityBattlePassRealtimeClock>(Lifetime.Singleton);
            builder.Register<IBattlePassTimerService, BattlePassTimerService>(Lifetime.Singleton);
            builder.Register<BattlePassUiModelFactory>(Lifetime.Singleton);
            builder.Register<IBattlePassServerService, BattlePassServerService>(Lifetime.Singleton);
        }
    }
}
