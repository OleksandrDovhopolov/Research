using System;
using System.Collections.Generic;
using EventOrchestration.Abstractions;

namespace EventOrchestration.Core
{
    public sealed class EventRegistry : IEventRegistry
    {
        private readonly Dictionary<string, IEventController> _controllers = new();

        public void Register(IEventController controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }

            if (string.IsNullOrWhiteSpace(controller.EventType))
            {
                throw new ArgumentException("EventType must be provided by controller.", nameof(controller));
            }

            _controllers[controller.EventType] = controller;
        }

        public bool TryGet(string eventType, out IEventController controller)
        {
            if (string.IsNullOrWhiteSpace(eventType))
            {
                controller = null;
                return false;
            }

            return _controllers.TryGetValue(eventType, out controller);
        }
    }
}
