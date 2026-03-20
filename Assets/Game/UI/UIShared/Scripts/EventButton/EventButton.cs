using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EventOrchestration.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIShared
{
    public class EventButton : MonoBehaviour, IEventButton
    {
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _timerText;
        
        private CancellationTokenSource _timerCts;
        
        public void Setup(ScheduleItem config, Action onClick, CancellationToken ct)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick?.Invoke());

            StartTimer(config.EndTimeUtc, ct).Forget();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private async UniTaskVoid StartTimer(DateTimeOffset endTime, CancellationToken externalCt)
        {
            _timerCts?.Cancel();
            _timerCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt, this.GetCancellationTokenOnDestroy());
            var ct = _timerCts.Token;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var remaining = endTime - DateTimeOffset.UtcNow;

                    if (remaining.TotalSeconds <= 0)
                    {
                        _timerText.text = "00:00:00";
                        break;
                    }

                    _timerText.text = FormatTime(remaining);

                    await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: ct);
                }
            }
            catch (OperationCanceledException) { }
        }
        
        private string FormatTime(TimeSpan ts)
        {
            if (ts.TotalDays >= 1)
                return $"{ts.Days}d {ts.Hours}h";
        
            return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
        
        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
            
            _timerCts?.Cancel();
            _timerCts?.Dispose();
        }
    }
}