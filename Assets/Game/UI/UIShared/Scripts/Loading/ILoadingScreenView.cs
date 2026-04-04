using System.Threading;
using Cysharp.Threading.Tasks;

namespace UIShared.Loading
{
    public interface ILoadingScreenView
    {
        void SetVisible(bool isVisible);
        void SetProgress(float normalizedProgress);
        void SetStatus(string status);
        void SetError(string message);
        void SetErrorVisible(bool isVisible);
        UniTask SetLoginButtonsVisibleAsync(bool isVisible, float durationSeconds, CancellationToken ct);
        UniTask<LoginSelectionMethod?> WaitForLoginSelectionAsync(CancellationToken ct);
        UniTask WaitForRetryClickAsync(CancellationToken ct);
    }
}
