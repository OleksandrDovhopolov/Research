using System;

namespace Inventory.API
{
    public static class InventoryCompositionRegistry
    {
        private static readonly object SyncRoot = new();
        private static IInventoryCompositionRoot _compositionRoot;

        public static bool IsRegistered => _compositionRoot != null;

        public static void Register(IInventoryCompositionRoot compositionRoot)
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
                        "Inventory composition root is already registered. Register it only once at startup.");
                }

                _compositionRoot = compositionRoot;
            }
        }

        public static IInventoryCompositionRoot Resolve()
        {
            if (_compositionRoot != null)
            {
                return _compositionRoot;
            }

            throw new InvalidOperationException(
                "Inventory composition root is not registered. Ensure implementation bootstrap runs before usage.");
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
