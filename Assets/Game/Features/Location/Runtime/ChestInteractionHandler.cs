namespace Game.Features.Locations
{
    public sealed class ChestInteractionHandler : ILocationInteractionHandler
    {
        private readonly ILocationInteractionUiGateway _uiGateway;

        public ChestInteractionHandler(ILocationInteractionUiGateway uiGateway)
        {
            _uiGateway = uiGateway;
        }

        public void Handle(LocationInteractionContext context)
        {
            _uiGateway.OpenChestItems(context);
        }
    }
}
