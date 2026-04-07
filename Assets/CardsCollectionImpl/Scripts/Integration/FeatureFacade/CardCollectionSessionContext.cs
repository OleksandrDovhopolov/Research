using System;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public sealed class CardCollectionSessionContext
    {
        public IOpenPackFlow OpenPackFlow { get; }
        public ICardCollectionWindowCoordinator WindowCoordinator { get; }
        public ICardCollectionApplicationFacade CardCollectionFacade { get; }

        public CardCollectionSessionContext(
            IOpenPackFlow openPackFlow,
            ICardCollectionWindowCoordinator windowCoordinator,
            ICardCollectionApplicationFacade cardCollectionFacade)
        {
            WindowCoordinator = windowCoordinator ?? throw new ArgumentNullException(nameof(windowCoordinator));
            CardCollectionFacade = cardCollectionFacade ?? throw new ArgumentNullException(nameof(cardCollectionFacade));
            OpenPackFlow = openPackFlow ?? throw new ArgumentNullException(nameof(openPackFlow));
        }
    }
}
