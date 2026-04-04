using System;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public sealed class CardCollectionSessionContext
    {
        public ICardCollectionModule Module { get; }
        public ICardCollectionPointsAccount PointsAccount { get; }
        public ICardCollectionWindowCoordinator WindowCoordinator { get; }

        public CardCollectionSessionContext(
            ICardCollectionModule module,
            ICardCollectionPointsAccount pointsAccount,
            ICardCollectionWindowCoordinator windowCoordinator)
        {
            Module = module ?? throw new ArgumentNullException(nameof(module));
            PointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
            WindowCoordinator = windowCoordinator ?? throw new ArgumentNullException(nameof(windowCoordinator));
        }
    }
}
