using System;

namespace CardCollection.Core
{
    public static class CardCollectionCompositionRegistry
    {
        private static readonly object SyncRoot = new();
        private static ICardCollectionCompositionRoot _compositionRoot;
        
        public static bool IsRegistered => _compositionRoot != null;

        public static void Register(ICardCollectionCompositionRoot compositionRoot)
        {
            if (compositionRoot == null)
            {
                throw new ArgumentNullException(nameof(compositionRoot));
            }

            lock (SyncRoot)
            {
                if (_compositionRoot == compositionRoot)
                {
                    return;
                }

                if (_compositionRoot != null)
                {
                    throw new InvalidOperationException(
                        "CardCollection composition root is already registered. " +
                        "Register it only once during application bootstrap.");
                }

                _compositionRoot = compositionRoot;
            }
        }

        public static bool TryResolve(out ICardCollectionCompositionRoot compositionRoot)
        {
            compositionRoot = _compositionRoot;
            return compositionRoot != null;
        }

        public static ICardCollectionCompositionRoot Resolve()
        {
            if (TryResolve(out var compositionRoot))
            {
                return compositionRoot;
            }

            throw new InvalidOperationException(
                "CardCollection composition root is not registered. " +
                "Ensure an implementation assembly registers it before game startup.");
        }

#if UNITY_EDITOR
        public static void ResetForTests()
        {
            lock (SyncRoot)
            {
                _compositionRoot = null;
            }
        }
#endif
    }
}
