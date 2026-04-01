using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface ICardCollectionWindowOpener
    {
        void OpenNewCardWindow(string packId);
        UniTask OpenCardCollectionWindow(CancellationToken ct);
        UniTask OpenCardGroupCompletedWindow(IEnumerable<string> groupType, CancellationToken ct);
    }
}
