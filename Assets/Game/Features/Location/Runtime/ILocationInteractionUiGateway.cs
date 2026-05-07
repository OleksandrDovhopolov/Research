namespace Game.Features.Locations
{
    public interface ILocationInteractionUiGateway
    {
        void OpenFishingTackleSelection(LocationInteractionContext context);
        void OpenFisherHouseProduction(LocationInteractionContext context);
        void OpenChestItems(LocationInteractionContext context);
        void OpenFishCollection(LocationInteractionContext context);
        void OpenCustom(LocationInteractionContext context);
    }
}
