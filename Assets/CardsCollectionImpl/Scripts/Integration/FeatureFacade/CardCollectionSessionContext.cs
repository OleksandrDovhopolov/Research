using System;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public sealed class CardCollectionSessionContext
    {
        public ICardCollectionUpdater Updater { get; }
        public ICardCollectionReader Reader { get; }
        public ICardCollectionPointsAccount PointsAccount { get; }

        public CardCollectionSessionContext(
            ICardCollectionUpdater updater,
            ICardCollectionReader reader,
            ICardCollectionPointsAccount pointsAccount)
        {
            Updater = updater ?? throw new ArgumentNullException(nameof(updater));
            Reader = reader ?? throw new ArgumentNullException(nameof(reader));
            PointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
        }
    }
}
