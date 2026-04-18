using System;
using System.Threading;

namespace FortuneWheel
{
    public interface IFortuneWheelTimerService
    {
        event Action<TimeSpan> OnTimerUpdated;
        event Action<FortuneWheelDataServerItem> OnStateUpdated;

        void Start(FortuneWheelDataServerItem initialData, CancellationToken ct);
        void ApplySpinResult(FortuneWheelSpinResult spinResult);
        void Stop();
    }
}
