using System;
using System.Collections.Generic;
using System.Linq;
using CardCollection.Core;

namespace CardCollectionImpl
{
    public sealed class CardCollectionSessionContext
    {
        public IOpenPackFlow OpenPackFlow { get; }
        public ICardCollectionWindowCoordinator WindowCoordinator { get; }
        public ICardCollectionApplicationFacade CardCollectionFacade { get; }
        public IPendingGroupCompletionPresentationQueue GroupCompletionPresentationQueue { get; }

        public CardCollectionSessionContext(
            IOpenPackFlow openPackFlow,
            ICardCollectionWindowCoordinator windowCoordinator,
            ICardCollectionApplicationFacade cardCollectionFacade,
            IPendingGroupCompletionPresentationQueue groupCompletionPresentationQueue)
        {
            WindowCoordinator = windowCoordinator ?? throw new ArgumentNullException(nameof(windowCoordinator));
            CardCollectionFacade = cardCollectionFacade ?? throw new ArgumentNullException(nameof(cardCollectionFacade));
            OpenPackFlow = openPackFlow ?? throw new ArgumentNullException(nameof(openPackFlow));
            GroupCompletionPresentationQueue = groupCompletionPresentationQueue ?? throw new ArgumentNullException(nameof(groupCompletionPresentationQueue));
        }
    }
}
