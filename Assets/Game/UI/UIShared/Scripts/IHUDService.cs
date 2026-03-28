namespace UIShared
{
    public interface IHUDService
    {
        IEventButton SpawnEventButton(string eventId);
        void RemoveEventButton(string eventId);
    }
}