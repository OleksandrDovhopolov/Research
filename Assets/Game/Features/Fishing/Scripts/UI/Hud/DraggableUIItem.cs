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
            Debug.LogWarning($"[DraggableUIItem] '{name}' disabled. Handlers will be cleared.");
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
            if (_onPointerDownHandler == null)
                Debug.LogWarning($"[DraggableUIItem] '{name}' received pointer down, but no pointer-down handler is assigned.");

            _onPointerDownHandler?.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_onPointerUpHandler == null)
                Debug.LogWarning($"[DraggableUIItem] '{name}' received pointer up, but no pointer-up handler is assigned.");

            _onPointerUpHandler?.Invoke(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.LogWarning($"[DraggableUIItem] '{name}' begin drag. Locked={_isDragLocked}, HasHandler={_onBeginDragAction != null}.");
            if (_isDragLocked)
            {
                Debug.LogWarning($"[DraggableUIItem] '{name}' begin drag ignored because drag is locked.");
                _onLockedBeginDragAction?.Invoke(eventData);
                return;
            }

            if (_onBeginDragAction == null)
                Debug.LogWarning($"[DraggableUIItem] '{name}' begin drag has no handler.");

            _onBeginDragAction?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isDragLocked)
            {
                Debug.LogWarning($"[DraggableUIItem] '{name}' drag ignored because drag is locked.");
                return;
            }

            if (_onDragAction == null)
                Debug.LogWarning($"[DraggableUIItem] '{name}' drag has no handler.");

            _onDragAction?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.LogWarning($"[DraggableUIItem] '{name}' end drag. Locked={_isDragLocked}, HasHandler={_onEndDragAction != null}.");
            if (_isDragLocked)
            {
                Debug.LogWarning($"[DraggableUIItem] '{name}' end drag ignored because drag is locked.");
                return;
            }

            if (_onEndDragAction == null)
                Debug.LogWarning($"[DraggableUIItem] '{name}' end drag has no handler.");

            _onEndDragAction?.Invoke(eventData);
        }
    }
}
