using UnityEngine.Events;

namespace TileEditor
{
    public class EnumListenerHelper : EnumPropertyHelper
    {
        public UnityEvent[] enumEvents;

        protected override void OnValueChange(int id)
        {
            enumEvents[id]?.Invoke();
        }
    }
}