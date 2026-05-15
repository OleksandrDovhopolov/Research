using System;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class ObjectInspectorMedium : MonoBehaviour
    {
        [SerializeField] private Text _objectName;
        [SerializeField] private Image _objectPreview;
        [SerializeField] private SimpleButton _inspectButton;
        [SerializeField] private SimpleButton _highlightButton;
        [SerializeField] private SimpleButton _moveToButton;
        [SerializeField] private SimpleButton _removeButton;

        public void Init(
            LocationObject obj, 
            Action onInspectAction, 
            Action onHighlightAction, 
            Action onMoveToAction,
            Action onRemoveAction)
        {
            _objectName.text = obj.Name;
            _objectPreview.sprite = obj.GetPreviewSprite();
            _inspectButton.Init(onInspectAction.Invoke);
            _highlightButton.Init(onHighlightAction.Invoke);
            _moveToButton.Init(onMoveToAction.Invoke);
            _removeButton.Init(onRemoveAction.Invoke);
        }
    }
}
