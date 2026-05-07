namespace Game.Features.Locations
{
    public interface ILocationInteractionHandler
    {
        bool CanHandle(LocationInteractionContext context);
        void Handle(LocationInteractionContext context);
    }
}
