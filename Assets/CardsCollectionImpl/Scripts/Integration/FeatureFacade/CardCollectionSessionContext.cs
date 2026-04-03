using System;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public sealed class CardCollectionSessionContext
    {
        public ICardCollectionModule Module { get; }
        public ICardCollectionPointsAccount PointsAccount { get; }
        public ICardCollectionWindowOpener WindowOpener { get; }

        public CardCollectionSessionContext(
            ICardCollectionModule module,
            ICardCollectionPointsAccount pointsAccount,
            ICardCollectionWindowOpener windowOpener)
        {
            Module = module ?? throw new ArgumentNullException(nameof(module));
            PointsAccount = pointsAccount ?? throw new ArgumentNullException(nameof(pointsAccount));
            WindowOpener = windowOpener ?? throw new ArgumentNullException(nameof(windowOpener));
        }
    }
}
