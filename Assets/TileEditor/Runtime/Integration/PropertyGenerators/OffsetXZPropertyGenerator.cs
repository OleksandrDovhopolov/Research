using UnityEngine;

[RequireComponent(typeof(LocationObject))]
public class OffsetXZPropertyGenerator : MonoBehaviour
{
    [SerializeField] private string _namePrefix;
    [SerializeField] private Transform _viewTransform;

    [SerializeField] private Vector2 _xLimits = new Vector2(-3.055f, 3.055f);
    [SerializeField] private Vector2 _zLimits = new Vector2(-3.005f, 3.005f);
    [SerializeField] private float _defaultXOffset;
    [SerializeField] private float _defaultZOffset;

    private TransformHelper _transformHelper;

    void Awake()
    {
        _transformHelper = _viewTransform.gameObject.GetComponent<TransformHelper>() ?? _viewTransform.gameObject.AddComponent<TransformHelper>();

        var offsetXProperty = gameObject.AddComponent<FloatRangedProperty>();
        offsetXProperty.SetPropertyName($"{_namePrefix}OffsetX");
        offsetXProperty.SetMinValue(_xLimits.x);
        offsetXProperty.SetMaxValue(_xLimits.y);
        offsetXProperty.onValueChangeEvent.AddListener(UpdateXOffset);
        offsetXProperty.SetValue(_defaultXOffset);

        var offsetYProperty = gameObject.AddComponent<FloatRangedProperty>();
        offsetYProperty.SetPropertyName($"{_namePrefix}OffsetZ");
        offsetYProperty.SetMinValue(_zLimits.x);
        offsetYProperty.SetMaxValue(_zLimits.y);
        offsetYProperty.onValueChangeEvent.AddListener(UpdateZOffset);
        offsetYProperty.SetValue(_defaultZOffset);
    }

    private void UpdateXOffset(float offset) => _transformHelper.SetLocalX(offset);
    private void UpdateZOffset(float offset) => _transformHelper.SetLocalZ(offset);
}