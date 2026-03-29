using System;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public sealed class CardCollectionSessionContext
    {
        public string CollectionId { get; }
        public ICardCollectionUpdater Updater { get; }
        public ICardCollectionReader Reader { get; }
        public ICardCollectionPointsAccount PointsAccount { get; }
        public ICardCollectionWindowOpener WindowOpener { get; }

        public CardCollectionSessionContext(
            string collectionId,
            ICardCollectionUpdater updater,
            ICardCollectionReader reader,
            ICardCollectionPointsAccount pointsAccount,
            ICardCollectionWindowOpener windowOpener)
        {
            CollectionId = collectionId;
            Updater = updater ?? throw new ArgumentNullException(nameof(updater));
            Reader = reader ?? throw new ArgumentNullException(nameof(reader));
            PointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
            WindowOpener = windowOpener ?? throw new ArgumentNullException(nameof(windowOpener));
        }
    }
}
