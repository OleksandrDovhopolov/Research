using UnityEngine;

namespace Fabros.TileEditor
{
    public partial class TileEditor : MonoBehaviour
    {
        [SerializeField] private GameObject _locationsPanel;
        [SerializeField] private ActiveLocationPanel _activeLocationPanel;
        [SerializeField] private BrushInspectorPanel _brushInspectorPanel;
        [SerializeField] private ObjectsListInspectorPanel _objectsListInspector;

        private RectTransform _canvasRectTransform;

        private static TileEditor _instance;

        public static TileEditor GetInstance() => _instance;

        public static bool IsEditorMode => GetInstance() != null;

        void Awake()
        {
            _instance = this;
            _canvasRectTransform = GetComponentInChildren<Canvas>().GetComponent<RectTransform>();
            hintContainer.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            _instance = null;
        }

        void Update()
        {
            UpdateInput();
        }
    }
}