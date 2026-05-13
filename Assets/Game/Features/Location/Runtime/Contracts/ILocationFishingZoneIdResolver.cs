namespace Game.Features.Locations
{
    public interface ILocationFishingZoneIdResolver
    {
        string ResolveZoneId(ILocationInteractable interactable);
    }
}
