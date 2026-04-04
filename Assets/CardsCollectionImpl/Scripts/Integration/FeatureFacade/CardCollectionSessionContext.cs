using System;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public sealed class CardCollectionSessionContext
    {
        public ICardCollectionModule Module { get; }
        public ICardCollectionPointsAccount PointsAccount { get; }
        public ICardCollectionCacheService CacheService { get; }
        public ICardCollectionWindowCoordinator WindowCoordinator { get; }

        public CardCollectionSessionContext(
            ICardCollectionModule module,
            ICardCollectionPointsAccount pointsAccount,
            ICardCollectionCacheService cacheService,
            ICardCollectionWindowCoordinator windowCoordinator)
        {
            Module = module ?? throw new ArgumentNullException(nameof(module));
            PointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
            CacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            WindowCoordinator = windowCoordinator ?? throw new ArgumentNullException(nameof(windowCoordinator));
        }
    }
}
