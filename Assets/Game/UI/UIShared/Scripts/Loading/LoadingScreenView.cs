using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIShared.Loading
{
    public sealed class LoadingScreenView : MonoBehaviour, ILoadingScreenView
    {
        [SerializeField] private CanvasGroup _rootGroup;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TextMeshProUGUI _statusText;

        private void Awake()
        {
            SetVisible(true);
            SetErrorVisible(false);
        }

        public void SetVisible(bool isVisible)
        {
            if (_rootGroup != null)
            {
                _rootGroup.alpha = isVisible ? 1f : 0f;
                _rootGroup.interactable = isVisible;
                _rootGroup.blocksRaycasts = isVisible;
            }
            else
            {
                gameObject.SetActive(isVisible);
            }
        }

        public void SetProgress(float normalizedProgress)
        {
            if (_progressBar != null)
            {
                _progressBar.value = Mathf.Clamp01(normalizedProgress);
            }
        }

        public void SetStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status ?? string.Empty;
            }
        }

        public void SetError(string message)
        {
        }

        public void SetErrorVisible(bool isVisible)
        {
        }

        public UniTask SetLoginButtonsVisibleAsync(bool isVisible, float durationSeconds, CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }

        public async UniTask<LoginSelectionMethod?> WaitForLoginSelectionAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            //var tcs = new UniTaskCompletionSource<LoginSelectionMethod?>();

            await UniTask.CompletedTask;
            Debug.LogWarning($"[Debug] LoginSelectionMethod.Guest returned");
            return LoginSelectionMethod.Guest;
            //await using var registration = ct.Register(() => tcs.TrySetCanceled(ct));
            //return await tcs.Task;
        }

        public async UniTask WaitForRetryClickAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var tcs = new UniTaskCompletionSource<bool>();

            Debug.LogError($"[Debug] Locked in WaitForRetryClickAsync");
            await using var registration = ct.Register(() => tcs.TrySetCanceled(ct));
            await tcs.Task;
        }
    }
}
