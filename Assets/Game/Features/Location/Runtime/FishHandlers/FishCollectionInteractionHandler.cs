namespace Game.Features.Locations
{
    public sealed class FishCollectionInteractionHandler : ILocationInteractionHandler
    {
        private readonly ILocationInteractionUiGateway _uiGateway;

        public FishCollectionInteractionHandler(ILocationInteractionUiGateway uiGateway)
        {
            _uiGateway = uiGateway;
        }

        public void Handle(LocationInteractionContext context)
        {
            _uiGateway.OpenFishCollection(context);
        }
    }
}
