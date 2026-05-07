using UnityEngine;
using UnityEngine.Events;

namespace Fabros.TileEditor
{
    public class ObjectScaleHelper : MonoBehaviour
    {
        [SerializeField] private Transform _viewTransform;
        
        [Range(0.01f, 0.99f)]
        [SerializeField] private float _minScale = 0.4f;
        [Range(1.1f, 10f)]
        [SerializeField] private float _maxScale = 2;
        
        private LocationObject _locationObject;
        private TransformHelper _transformHelper;
        private FloatRangedProperty _scaleOffsetProperty;

        private void Awake()
        {
            ConfigureHelpers();
        }

        void Start()
        {
            if (_viewTransform == null) return;
            
            _locationObject = GetComponent<LocationObject>();
            
            if (!TileEditor.IsEditorMode || !_locationObject.IsBrushMode) return;
            
            _locationObject.OnBeforeBrush += BeforeBrushHandler;
            _locationObject.OnAfterBrush += AfterBrushHandler;
        }

        void OnDestroy()
        {
            if (_locationObject != null)
            {
                _locationObject.OnBeforeBrush -= BeforeBrushHandler;
                _locationObject.OnAfterBrush -= AfterBrushHandler;
            }
        }
        
        private void UpdateOffset(float offset) => _transformHelper.SetScale(offset);
        
        private void ConfigureHelpers()
        {
            if (_viewTransform == null) return;
            
            _transformHelper = _viewTransform.gameObject.AddComponent<TransformHelper>();
            _scaleOffsetProperty = GenerateProperty("Scale", UpdateOffset, _minScale, _maxScale);
        }
        
        private FloatRangedProperty GenerateProperty(string id, UnityAction<float> listener, float minValue, float maxValue, float defaultValue = 1)
        {
            var property = gameObject.AddComponent<FloatRangedProperty>();
            property.SetPropertyName(id);
            property.SetMinValue(minValue);
            property.SetMaxValue(maxValue);
            property.onValueChangeEvent.AddListener(listener);
            property.SetValue(defaultValue);

            return property;
        }
        
        private void BeforeBrushHandler(EditorTileCell obj)
        {
            var localOffset = TileEditor.GetInstance().GetBrushContainerPosition() - obj.transform.localScale;
            _scaleOffsetProperty.SetValue(localOffset);
        }

        private void AfterBrushHandler(EditorTileCell obj)
        {
            _scaleOffsetProperty.SetValue(1);
        }
    }
}