using Cysharp.Threading.Tasks;
using Game.Fishing;

namespace Game.Features.Locations
{
    public sealed class FishingZoneInteractionHandler : ILocationInteractionHandler
    {
        private readonly IFishingZoneInfoLogger _zoneInfoLogger;
        private readonly ILocationFishingZoneIdResolver _zoneIdResolver;

        public FishingZoneInteractionHandler(
            IFishingZoneInfoLogger zoneInfoLogger,
            ILocationFishingZoneIdResolver zoneIdResolver)
        {
            _zoneInfoLogger = zoneInfoLogger;
            _zoneIdResolver = zoneIdResolver;
        }

        public void Handle(LocationInteractionContext context)
        {
            _zoneInfoLogger.LogZoneInfoAsync(_zoneIdResolver.ResolveZoneId(context.Interactable)).Forget();
        }
    }
}
