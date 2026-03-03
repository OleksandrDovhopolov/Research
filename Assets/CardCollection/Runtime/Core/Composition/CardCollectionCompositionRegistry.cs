using System;

namespace CardCollection.Core
{
    public static class CardCollectionCompositionRegistry
    {
        private static ICardCollectionCompositionRoot _compositionRoot;

        public static void Register(ICardCollectionCompositionRoot compositionRoot)
        {
            _compositionRoot = compositionRoot ?? throw new ArgumentNullException(nameof(compositionRoot));
        }

        public static ICardCollectionCompositionRoot Resolve()
        {
            if (_compositionRoot == null)
            {
                throw new InvalidOperationException(
                    "CardCollection composition root is not registered. " +
                    "Ensure an implementation assembly registers it before game startup.");
            }

            return _compositionRoot;
        }
    }
}
