using System;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class ObjectInspectorLite : MonoBehaviour
    {
        [SerializeField] private Text _objectName;
        [SerializeField] private Image _objectPreview;
        [SerializeField] private SimpleButton _removeButton;
        [SerializeField] private Button _inspectButton;

        public void Init(LocationObject obj, Action onRemoveObjectAction, Action onInspectObjectAction)
        {
            _objectName.text = obj.Name;
            _objectPreview.sprite = obj.GetPreviewSprite();
            _removeButton.Init(onRemoveObjectAction.Invoke);
            _inspectButton.onClick.AddListener(() => onInspectObjectAction?.Invoke());
        }

        public void SetRemoveButtonText(string text) => _removeButton.SetText(text);
    }
}
