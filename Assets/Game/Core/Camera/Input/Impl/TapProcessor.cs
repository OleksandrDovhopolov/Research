using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using VContainer;

namespace InputSystem
{
    public class TapProcessor : BaseProcessor, ITapProcessor
    {
        /*private ITapInputHandler _handler;

        private readonly Filter<ITapInputHandler> _filter = new();

        private Raycaster _raycaster;
        private ScreenPointConverter _screenPointConverter;

        [Inject]
        public void Install(Raycaster raycaster, ScreenPointConverter screenPointConverter)
        {
            _raycaster = raycaster;
            _screenPointConverter = screenPointConverter;

            _filter.Init();
        }*/

        public IFilterBuilder AddCommand<TCommand>(TCommand command) where TCommand : ITapInputHandler, IFilterNode
        {
            throw new NotImplementedException("Not implemented");
            //return _filter.AddCommand(command);
        }

        public void OnPointerDown(Vector2 position)
        {
            /*var (canvasHit, worldHit) = IsUIPressed(out _);
            if (canvasHit) return;
            
            _handler = _filter.FindHandler(_raycaster.Raycast(position));

            if (_handler == null) return;
            if (worldHit && !_handler.IsOverWorldCanvas)
            {
                _handler = null;
                return; 
            }
            
            _handler?.OnPointerDown();*/
        }

        public void OnHold(Vector2 position, Vector2 delta) { }

        public void OnPointerUp(Vector2 position)
        {
            /*_handler?.InPointerUp();
            var worldPos = _screenPointConverter.ScreenToWorld(position);
            _handler?.OnTap(worldPos);*/
        }

        public void Cancel() 
        { 
            //_handler?.InPointerUp();
        }

        public void Dispose()
        {
            //_handler = null;
        }
    }
}