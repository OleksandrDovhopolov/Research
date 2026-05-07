using Fabros.TileEditor;
using UnityEngine;

[RequireComponent(typeof(LocationObject))]
public class OffsetXYPropertyGenerator : MonoBehaviour
{
    [SerializeField] private Transform _viewTransform;

    [SerializeField] private Vector2 _xLimits = new Vector2(-1.055f, 1.055f);
    [SerializeField] private Vector2 _yLimits;
    [SerializeField] private float _defaultXOffset;
    [SerializeField] private float _defaultYOffset;

    private TransformHelper _transformHelper;

    void Awake()
    {
        _transformHelper = _viewTransform.gameObject.AddComponent<TransformHelper>();

        var offsetXProperty = gameObject.AddComponent<FloatRangedProperty>();
        offsetXProperty.SetPropertyName("OffsetX");
        offsetXProperty.SetMinValue(_xLimits.x);
        offsetXProperty.SetMaxValue(_xLimits.y);
        offsetXProperty.onValueChangeEvent.AddListener(UpdateXOffset);
        offsetXProperty.SetValue(_defaultXOffset);

        var offsetYProperty = gameObject.AddComponent<FloatRangedProperty>();
        offsetYProperty.SetPropertyName("OffsetY");
        offsetYProperty.SetMinValue(_yLimits.x);
        offsetYProperty.SetMaxValue(_yLimits.y);
        offsetYProperty.onValueChangeEvent.AddListener(UpdateYOffset);
        offsetYProperty.SetValue(_defaultYOffset);
    }

    private void UpdateXOffset(float offset) => _transformHelper.SetLocalX(offset);
    private void UpdateYOffset(float offset) => _transformHelper.SetLocalY(offset);
}
