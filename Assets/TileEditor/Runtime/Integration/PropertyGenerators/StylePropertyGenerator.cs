using TileEditor;
using UnityEngine;

[RequireComponent(typeof(LocationObject))]
public class StylePropertyGenerator : BaseStylePropertyGenerator
{
    [SerializeField] protected Sprite[] _styleSprites;
    [SerializeField] protected SpriteRenderer _mainRenderer;

    protected override void ChangeStyle(int styleId)
    {
        _mainRenderer.sprite = _styleSprites[styleId];
    }
}
