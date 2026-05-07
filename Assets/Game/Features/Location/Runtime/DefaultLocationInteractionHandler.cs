namespace Game.Features.Locations
{
    public sealed class DefaultLocationInteractionHandler : ILocationInteractionHandler
    {
        private readonly ILocationInteractionUiGateway _uiGateway;

        public DefaultLocationInteractionHandler(ILocationInteractionUiGateway uiGateway)
        {
            _uiGateway = uiGateway;
        }

        public bool CanHandle(LocationInteractionContext context)
        {
            return context.Interactable != null;
        }

        public void Handle(LocationInteractionContext context)
        {
            switch (context.InteractionType)
            {
                case LocationInteractionType.FishingZone:
                    _uiGateway.OpenFishingTackleSelection(context);
                    break;
                case LocationInteractionType.FisherHouse:
                    _uiGateway.OpenFisherHouseProduction(context);
                    break;
                case LocationInteractionType.Chest:
                    _uiGateway.OpenChestItems(context);
                    break;
                case LocationInteractionType.FishCollection:
                    _uiGateway.OpenFishCollection(context);
                    break;
                default:
                    _uiGateway.OpenCustom(context);
                    break;
            }
        }
    }
}
