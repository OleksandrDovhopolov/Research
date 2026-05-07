using UnityEngine.Events;

namespace Fabros.TileEditor
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