using UnityEngine;
using UnityEngine.Events;

namespace TileEditor
{
    public class IntegerListenerHelper : MonoBehaviour
    {
        public UnityEvent[] onValuesEvents;

        public void Apply(int value) => onValuesEvents[value].Invoke();
    }
}