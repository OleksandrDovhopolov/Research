using UnityEngine;

namespace core
{

    public class SafeAreaLayout : MonoBehaviour
    {
        private RectTransform _panel;
        private Rect _lastSafeArea;

        private void OnEnable()
        {
            _lastSafeArea = new Rect(0, 0, 0, 0);
            _panel = GetComponent<RectTransform>();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            Rect safeArea = GetSafeArea();

            if (safeArea != _lastSafeArea) ApplySafeArea(safeArea);
        }

        private Rect GetSafeArea()
        {
            var area = Screen.safeArea;
            return area;
        }

        private void ApplySafeArea(Rect r)
        {
            _lastSafeArea = r;

            var anchorMin = r.position;
            var anchorMax = r.position + r.size;
            anchorMin.x /= Screen.width;
            anchorMax.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.y /= Screen.height;

            _panel.anchorMin = anchorMin;
            _panel.anchorMax = anchorMax;
        }
    }
}