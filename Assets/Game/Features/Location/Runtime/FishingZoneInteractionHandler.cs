using Cysharp.Threading.Tasks;
using Game.Fishing;

namespace Game.Features.Locations
{
    public sealed class FishingZoneInteractionHandler : ILocationInteractionHandler
    {
        private readonly IFishingZoneInfoLogger _zoneInfoLogger;

        public FishingZoneInteractionHandler(IFishingZoneInfoLogger zoneInfoLogger)
        {
            _zoneInfoLogger = zoneInfoLogger;
        }

        public void Handle(LocationInteractionContext context)
        {
            _zoneInfoLogger.LogZoneInfoAsync(context.Interactable).Forget();
        }
    }
}
