using TileEditor;
using UnityEngine;

[RequireComponent(typeof(LocationObject))]
public class RotationPropertyGenerator : MonoBehaviour
{
    [SerializeField] private Transform _viewTransform;
    
    [SerializeField] private Vector2 _xLimits = new Vector2(-360f, 360f);
    [SerializeField] private Vector2 _yLimits = new Vector2(-360f, 360f);
    [SerializeField] private Vector2 _zLimits = new Vector2(-360f, 360f);
    [SerializeField] private float _defaultXOffset;
    [SerializeField] private float _defaultYOffset;
    [SerializeField] private float _defaultZOffset;
    
    private TransformHelper _transformHelper;
    
    void Awake()
    {
        _transformHelper = _viewTransform.gameObject.GetComponent<TransformHelper>() ?? _viewTransform.gameObject.AddComponent<TransformHelper>();

        var offsetXProperty = gameObject.AddComponent<FloatRangedProperty>();
        offsetXProperty.SetPropertyName("RotationX");
        offsetXProperty.SetMinValue(_xLimits.x);
        offsetXProperty.SetMaxValue(_xLimits.y);
        offsetXProperty.onValueChangeEvent.AddListener(UpdateX);
        offsetXProperty.SetValue(_defaultXOffset);

        var offsetYProperty = gameObject.AddComponent<FloatRangedProperty>();
        offsetYProperty.SetPropertyName("RotationY");
        offsetYProperty.SetMinValue(_yLimits.x);
        offsetYProperty.SetMaxValue(_yLimits.y);
        offsetYProperty.onValueChangeEvent.AddListener(UpdateY);
        offsetYProperty.SetValue(_defaultYOffset);
        
        var offsetZProperty = gameObject.AddComponent<FloatRangedProperty>();
        offsetZProperty.SetPropertyName("RotationZ");
        offsetZProperty.SetMinValue(_zLimits.x);
        offsetZProperty.SetMaxValue(_zLimits.y);
        offsetZProperty.onValueChangeEvent.AddListener(UpdateZ);
        offsetZProperty.SetValue(_defaultZOffset);
    }

    private void UpdateX(float offset) => _transformHelper.SetRotationX(offset);
    private void UpdateY(float offset) => _transformHelper.SetRotationY(offset);
    private void UpdateZ(float offset) => _transformHelper.SetRotationZ(offset);
}