using UnityEngine;
using UnityEngine.Events;

namespace TileEditor
{
    public class BooleanListenerHelper : MonoBehaviour
    {
        public UnityEvent onTrueEvent;
        public UnityEvent onFalseEvent;

        public void Apply(bool value) => (value ? onTrueEvent : onFalseEvent).Invoke();
    }
}