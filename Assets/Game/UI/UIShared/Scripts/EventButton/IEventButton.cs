using System;
using System.Threading;
using EventOrchestration.Models;

namespace UIShared
{
    public interface IEventButton
    {
        void Setup(ScheduleItem config, Action onClick, CancellationToken ct);
        void SetVisible(bool visible);
    }
}