using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Fishing
{
    public class DraggableUIItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private Action<PointerEventData> _onPointerDownHandler;
        private Action<PointerEventData> _onPointerUpHandler;
        private Action<PointerEventData> _onBeginDragAction;
        private Action<PointerEventData> _onLockedBeginDragAction;
        private Action<PointerEventData> _onDragAction;
        private Action<PointerEventData> _onEndDragAction;

        private bool _isDragLocked;

        public void OnDisable()
        {
            ClearHandlers();
        }

        public void ClearHandlers()
        {
            _onPointerDownHandler = null;
            _onPointerUpHandler = null;
            _onBeginDragAction = null;
            _onLockedBeginDragAction = null;
            _onDragAction = null;
            _onEndDragAction = null;
        }

        public void LockDrag() => _isDragLocked = true;
        public void UnlockDrag() => _isDragLocked = false;
        
        public DraggableUIItem WithPointerDownHandler(Action<PointerEventData> onPointerDownHandler)
        {
            _onPointerDownHandler = onPointerDownHandler;
            
            return this;
        }
        
        public DraggableUIItem WithPointerUpHandler(Action<PointerEventData> onPointerUpHandler)
        {
            _onPointerUpHandler = onPointerUpHandler;
            
            return this;
        }
        
        public DraggableUIItem WithBeginDragHandler(Action<PointerEventData> onBeginDragHandler)
        {
            _onBeginDragAction = onBeginDragHandler;
            UnlockDrag();
            
            return this;
        }

        public DraggableUIItem WithLockedBeginDragHandler(Action<PointerEventData> onLockedBeginDragHandler)
        {
            _onLockedBeginDragAction = onLockedBeginDragHandler;
            return this;
        }
        
        public DraggableUIItem WithDragHandler(Action<PointerEventData> onDragHandler)
        {
            _onDragAction = onDragHandler;
            UnlockDrag();
            
            return this;
        }
        
        public DraggableUIItem WithEndDragHandler(Action<PointerEventData> onEndDragHandler)
        {
            _onEndDragAction = onEndDragHandler;
            UnlockDrag();
            
            return this;
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            _onPointerDownHandler?.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _onPointerUpHandler?.Invoke(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isDragLocked)
            {
                _onLockedBeginDragAction?.Invoke(eventData);
                return;
            }

            _onBeginDragAction?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isDragLocked)
                return;

            _onDragAction?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isDragLocked)
                return;

            _onEndDragAction?.Invoke(eventData);
        }
    }
}
