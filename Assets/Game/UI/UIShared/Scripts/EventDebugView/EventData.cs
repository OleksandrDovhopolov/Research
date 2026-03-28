using UnityEngine;

namespace UIShared
{
    [CreateAssetMenu(fileName = "EventData", menuName = "Debug/Event Data", order = 0)]
    public class EventData : ScriptableObject
    {
        [SerializeField] private string eventId;
        [SerializeField] private Sprite eventIcon;

        public string EventId => eventId;
        public Sprite EventIcon => eventIcon;
    }
}
