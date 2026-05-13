using System;
using TMPro;
using UIShared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Fishing
{
    public sealed class LureView : MonoBehaviour, ICleanup
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private DraggableUIItem _draggableItem;

        public Sprite CurrentSprite => _icon != null ? _icon.sprite : null;
        public int CurrentCount { get; private set; }

        public void SetData(Sprite sprite, int count)
        {
            CurrentCount = count;
            SetSprite(sprite);
            SetText(_countText, count.ToString());
        }

        public void SetSprite(Sprite sprite)
        {
            if (_icon != null)
                _icon.sprite = sprite;
        }

        public void SetDragHandlers(
            Action<PointerEventData> onBeginDrag,
            Action<PointerEventData> onLockedBeginDrag,
            Action<PointerEventData> onDrag,
            Action<PointerEventData> onEndDrag)
        {
            if (_draggableItem == null)
                return;

            _draggableItem
                .WithBeginDragHandler(onBeginDrag)
                .WithLockedBeginDragHandler(onLockedBeginDrag)
                .WithDragHandler(onDrag)
                .WithEndDragHandler(onEndDrag);
        }

        public void SetDragLocked(bool isLocked)
        {
            if (_draggableItem == null)
                return;

            if (isLocked)
                _draggableItem.LockDrag();
            else
                _draggableItem.UnlockDrag();
        }

        public void Cleanup()
        {
            CurrentCount = 0;

            if (_icon != null)
                _icon.sprite = null;

            SetText(_countText, string.Empty);

            if (_draggableItem != null)
            {
                _draggableItem.ClearHandlers();
                _draggableItem.LockDrag();
            }
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null)
                label.text = value ?? string.Empty;
        }
    }
}
