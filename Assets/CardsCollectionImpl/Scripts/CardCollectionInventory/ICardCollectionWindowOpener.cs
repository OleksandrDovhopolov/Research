using System.Threading;
using CardCollection.Core;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface ICardCollectionWindowOpener
    {
        void OpenNewCardWindow(string packId);
        void OpenNewCardWindow(CardPack pack);
        UniTask OpenCardCollectionWindow(CancellationToken ct);
        UniTask OpenCardGroupCompletedWindow(string groupType, CancellationToken ct);
    }
}
