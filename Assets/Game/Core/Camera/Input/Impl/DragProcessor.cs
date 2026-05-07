using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace InputSystem
{
    public class DragProcessor : BaseProcessor, IDragProcessor
    {
        private static List<object> Empty = new();
        private Filter<IDragInputHandler> _dragFilter = new();
 
        private IDragInputHandler _dragHandler;
        
        //private Raycaster _raycaster;
        private ScreenPointConverter _screenPointConverter;

        [Inject]
        //public void Install(Raycaster raycaster, ScreenPointConverter screenPointConverter)
        public void Install(ScreenPointConverter screenPointConverter)
        {
            //_raycaster = raycaster;
            _screenPointConverter = screenPointConverter;
            
            _dragFilter.Init();
        }

        public void SetDragFilter(Filter<IDragInputHandler> dragFilter)
        {
            _dragFilter = dragFilter;
        }

        public void OnPointerDown(Vector2 position)
        {
            //_dragHandler = _dragFilter.FindHandler(_raycaster.Raycast(position));
            _dragHandler = _dragFilter.FindHandler(Empty);
            
            var worldPos = _screenPointConverter.ScreenToWorld(position);
            _dragHandler?.OnDragStart(worldPos);
        }

        public void OnHold(Vector2 position, Vector2 delta)
        {
            if (_dragHandler == null)
                return;
            
            var worldPos = _screenPointConverter.ScreenToWorld(position);
            var worldFrom =  _screenPointConverter.ScreenToWorld(position - delta);
            var worldDelta =  worldFrom - worldPos;
            _dragHandler.OnHold(worldPos, worldDelta);
            
            if (_dragHandler.TargetObject == null)
                return;
            
            position = _dragHandler.HandlePosition(position);
            worldPos = _screenPointConverter.ScreenToWorld(position);
        }

        public void OnPointerUp(Vector2 position)
        {
            var worldPos = _screenPointConverter.ScreenToWorld(position);
            
            _dragHandler?.OnDragEnd(worldPos);
        }

        public void Cancel()
        {
            _dragHandler?.OnCancel();
        }
        
        public void Dispose()
        {
            _dragHandler = null;
        }
    }
}