using System;
using System.Threading;
using EventOrchestration.Models;

namespace GameplayUI
{
    public interface IEventButton
    {
        void Setup(ScheduleItem config, Action onClick, CancellationToken ct);
        void SetVisible(bool visible);
    }
}