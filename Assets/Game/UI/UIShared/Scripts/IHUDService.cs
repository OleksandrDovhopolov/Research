namespace UIShared
{
    public interface IHUDService
    {
        IEventButton SpawnEventButton(string eventId, string spriteName);
        void RemoveEventButton(string eventId);
    }
}