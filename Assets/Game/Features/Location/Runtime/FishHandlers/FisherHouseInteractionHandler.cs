namespace Game.Features.Locations
{
    public sealed class FisherHouseInteractionHandler : ILocationInteractionHandler
    {
        private readonly ILocationInteractionUiGateway _uiGateway;

        public FisherHouseInteractionHandler(ILocationInteractionUiGateway uiGateway)
        {
            _uiGateway = uiGateway;
        }

        public void Handle(LocationInteractionContext context)
        {
            _uiGateway.OpenFisherHouseProduction(context);
        }
    }
}
