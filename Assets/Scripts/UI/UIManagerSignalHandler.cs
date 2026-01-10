using UISystem;
using UnityEngine;

namespace core
{
    public class UIManagerSignalHandler : UIManagerEventHandlerBase
    {
        public override void WindowShowEventInvoke(IWindowController window)
        {
            //Debug.LogWarning($"WindowShowEventInvoke");
        }

        public override void WindowHideEventInvoke(IWindowController window, bool isClosed)
        {
            //Debug.LogWarning($"WindowHideEventInvoke");
        }

        public override void WindowAnimationEventInvoke(IWindowController window, WindowAnimationType eventType)
        {
            //Debug.LogWarning($"WindowAnimationEventInvoke");
        }

        public override void StackCommandProcessedEventInvoke(UICommand uiCommand)
        {
            //Debug.LogWarning($"StackCommandProcessedEventInvoke");
        }

        public override void StackCommandProcessEventInvoke(UICommand uiCommand)
        {
            //Debug.LogWarning($"StackCommandProcessEventInvoke");
        }

        public override void StackCommandProcessAddEventInvoke(UICommand uiCommand)
        {
            //Debug.LogWarning($"StackCommandProcessAddEventInvoke");
        }
    }
}

