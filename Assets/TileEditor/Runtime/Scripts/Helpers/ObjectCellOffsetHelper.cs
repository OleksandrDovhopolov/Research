using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace TileEditor
{
    [RequireComponent(typeof(LocationObject))]
    public class ObjectCellOffsetHelper : MonoBehaviour
    {
        private const string XOffsetKey = "OffsetX";
        private const string YOffsetKey = "OffsetY";
        private const string ZOffsetKey = "OffsetZ";
        
        [SerializeField] private Transform _viewTransform;
        [SerializeField] private bool _XOffset = true;
        [SerializeField] private bool _YOffset = false;
        [SerializeField] private bool _ZOffset = true;

        private LocationObject _locationObject;
        private TransformHelper _transformHelper;
        private FloatRangedProperty _offsetXProperty;
        private FloatRangedProperty _offsetYProperty;
        private FloatRangedProperty _offsetZProperty;
        private bool _isExternalInited;

        private void Awake()
        {
            if (_isExternalInited)
            {
                return;
            }
            ConfigurateHelpers();
        }
        
        private void Start()
        {
            if (_viewTransform == null) return;
            
            _locationObject = GetComponent<LocationObject>();

            if (!TileEditor.IsEditorMode || !_locationObject.IsBrushMode) return;

            StartCoroutine(UpdatePositionRoutine());
            _locationObject.OnBeforeBrush += BeforeBrushHandler;
            _locationObject.OnAfterBrush += AfterBrushHandler;
        } 
        
        private void OnDestroy()
        {
            if (_locationObject == null)
            {
                return;
            }
            
            _locationObject.OnBeforeBrush -= BeforeBrushHandler;
            _locationObject.OnAfterBrush -= AfterBrushHandler;
        }

        private void UpdateXOffset(float offset) => _transformHelper.SetLocalX(offset);
        private void UpdateYOffset(float offset) => _transformHelper.SetLocalY(offset);
        private void UpdateZOffset(float offset) => _transformHelper.SetLocalZ(offset);

        public void SetTransform(Transform viewTransform)
        {
            _viewTransform = viewTransform;
            ConfigurateHelpers();
            _isExternalInited = true;
        }

        private void ConfigurateHelpers()
        {
            if (_viewTransform == null) return;
            
            _transformHelper = _viewTransform.gameObject.AddComponent<TransformHelper>();

            
            if (_XOffset)
            {
                _offsetXProperty = GenerateProperty(XOffsetKey, UpdateXOffset, -10, 10);
            }

            if (_YOffset)
            {
                _offsetYProperty = GenerateProperty(YOffsetKey, UpdateYOffset, -10, 10);
            }
            
            if (_ZOffset)
            {
                _offsetZProperty = GenerateProperty(ZOffsetKey, UpdateZOffset, -10, 10);
            }
        }
        
        private FloatRangedProperty GenerateProperty(string id, UnityAction<float> listener, float minValue, float maxValue, float defaultValue = 0)
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
            var localOffset = TileEditor.GetInstance().GetBrushContainerPosition() - obj.transform.position;

            if (_offsetXProperty != null)
            {
                _offsetXProperty.SetValue(localOffset.x);
            }

            if (_offsetYProperty != null)
            {
                _offsetYProperty.SetValue(localOffset.y);
            }

            if (_offsetZProperty != null)
            {
                _offsetZProperty.SetValue(localOffset.z);
            }
        }

        private void AfterBrushHandler(EditorTileCell obj)
        {
            if (_offsetXProperty != null)
            {
                _offsetXProperty.SetValue(0);
            }

            if (_offsetYProperty != null)
            {
                _offsetYProperty.SetValue(0);
            }

            if (_offsetZProperty != null)
            {
                _offsetZProperty.SetValue(0);
            }
        }

        private IEnumerator UpdatePositionRoutine()
        {
            while (true)
            {
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, 1000, 1 << 20))
                {
                    TileEditor.GetInstance().SetBrushContainerPosition(hit.point);
                }

                yield return new WaitForEndOfFrame();
            }
            // ReSharper disable once IteratorNeverReturns
        }

        private void Reset()
        {
            if (_viewTransform == null) _viewTransform = transform.GetChild(0);
        }
    }
}