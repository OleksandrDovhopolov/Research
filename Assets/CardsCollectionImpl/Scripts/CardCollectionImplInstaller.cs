using CardCollection.Core;
using UnityEngine;

namespace CardCollectionImpl
{
    public static class CardCollectionImplInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterCompositionRoot()
        {
            CardCollectionCompositionRegistry.Register(new CardCollectionImplCompositionRoot());
        }
    }
}
