using VContainer;

namespace FortuneWheel
{
    public static class FortuneWheelInstaller
    {
        public static void RegisterFortuneWheel(this IContainerBuilder builder)
        {
            builder.Register<IFortuneWheelServerService, FortuneWheelServerService>(Lifetime.Singleton);
        }
    }
}
