namespace Game.Features.Locations
{
    public sealed class FishingZoneInteractionHandler : ILocationInteractionHandler
    {
        private readonly ILocationInteractionUiGateway _uiGateway;

        public FishingZoneInteractionHandler(ILocationInteractionUiGateway uiGateway)
        {
            _uiGateway = uiGateway;
        }

        public void Handle(LocationInteractionContext context)
        {
            _uiGateway.OpenFishingTackleSelection(context);
        }
    }
}
