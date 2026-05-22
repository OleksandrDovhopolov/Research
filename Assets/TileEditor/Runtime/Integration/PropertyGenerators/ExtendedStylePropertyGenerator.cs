using TileEditor;
using UnityEngine;

public class ExtendedStylePropertyGenerator : StylePropertyGenerator
{
    [SerializeField] protected Sprite[] _extendedStyleSprites;

    private bool _useExtendedSprites;
    private int _spriteIndex;

    protected override void Awake()
    {
        base.Awake();

        var booleanProperty = gameObject.AddComponent<BooleanProperty>();
        booleanProperty.SetPropertyName("ExtendedVariant");
        booleanProperty.onValueChangeEvent.AddListener(b =>
        {
            if (_useExtendedSprites == b) return;
            _useExtendedSprites = b; DoChangeStyle();
        });

        booleanProperty.SetValue(false);
    }

    protected override void ChangeStyle(int arg0)
    {
        _spriteIndex = arg0;
        DoChangeStyle();
    }

    private void DoChangeStyle()
    {
        _mainRenderer.sprite = _useExtendedSprites ? _extendedStyleSprites[_spriteIndex] : _styleSprites[_spriteIndex];
    }
}
