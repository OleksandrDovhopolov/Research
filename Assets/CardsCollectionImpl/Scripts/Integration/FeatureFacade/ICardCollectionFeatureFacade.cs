using System.Threading;
using Cysharp.Threading.Tasks;

namespace CardCollectionImpl
{
    public interface ICardCollectionFeatureFacade
    {
        bool IsActive { get; }

        void SetActiveSession();
        void ClearSession();
        UniTask ShowCardCollectionWindow(CancellationToken ct);
        UniTask ShowNewCardWindow(string packId, CancellationToken ct);
    }
}