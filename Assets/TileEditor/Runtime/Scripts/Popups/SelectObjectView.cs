using System;
using UnityEngine;
using UnityEngine.UI;

namespace TileEditor
{
    public class SelectObjectView : MonoBehaviour
    {
        [SerializeField] private Image objectPreview;
        [SerializeField] private Text objectName;
        [SerializeField] private SimpleButton selectButton;

        public void Init(LocationObject availableObject, Action<LocationObject> onObjectSelectedHandler)
        {
            objectPreview.sprite = availableObject.GetPreviewSprite();
            objectName.text = availableObject.Name;
            selectButton.Init(() => onObjectSelectedHandler?.Invoke(availableObject));
        }
    }
}