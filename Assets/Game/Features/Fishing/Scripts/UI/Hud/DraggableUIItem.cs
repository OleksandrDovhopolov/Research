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
        private Action<PointerEventData> _onDragAction;
        private Action<PointerEventData> _onEndDragAction;

        [SerializeField] private Transform _draggableTarget;

        private bool _isDragLocked;
        
        public Transform DraggableTargetTransform => _draggableTarget;
        
        public void OnDisable()
        {
            ClearHandlers();
            ResetTarget();
        }

        public void ClearHandlers()
        {
            _onPointerDownHandler = null;
            _onPointerUpHandler = null;
            _onBeginDragAction = null;
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
            if (_isDragLocked) return;
            SetDragPreviewActive(true);
            _onBeginDragAction?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isDragLocked) return;
            if (_draggableTarget != null)
                _draggableTarget.transform.position = eventData.position;
            _onDragAction?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ResetTarget();
            
            if (_isDragLocked) return;
            _onEndDragAction?.Invoke(eventData);
        }

        private void ResetTarget()
        {
            if (_draggableTarget == null)
                return;

            _draggableTarget.transform.localPosition = Vector3.zero;
            SetDragPreviewActive(false);
        }

        private void SetDragPreviewActive(bool isActive)
        {
            if (_draggableTarget == null || _draggableTarget.childCount == 0)
                return;

            _draggableTarget.GetChild(0).gameObject.SetActive(isActive);
        }
    }
}
