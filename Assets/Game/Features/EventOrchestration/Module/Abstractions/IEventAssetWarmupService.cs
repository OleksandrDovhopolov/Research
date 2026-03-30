namespace EventOrchestration.Abstractions
{
    public interface IEventAssetWarmupService
    {
        void ReleaseAllWarmedAssets();
    }
}
