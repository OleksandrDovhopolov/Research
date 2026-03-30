using EventOrchestration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIShared
{
    public class EventDebugItemView : MonoBehaviour, ICleanup
    {
        [SerializeField] private TextMeshProUGUI idText;
        [SerializeField] private EventTimerDisplay _timerDisplay;
        [SerializeField] private Image icon;
        
        public Image Image => icon;

        public void SetData(string eventId, Sprite eventIcon, IGlobalTimerService  globalTimerService)
        {
            idText.text = eventId;
            icon.sprite = eventIcon;
            _timerDisplay.Bind(eventId , globalTimerService);
        }

        public void ResetSprite()
        {
            icon.sprite = null;
        }
        
        public void Cleanup()
        {
            _timerDisplay.Unbind();
        }
    }
}

