namespace UIShared
{
    public interface IHudWidgetLifecycle
    {
        void OnCreatedByHudController();
        void OnBeforeReleased();
    }
}
