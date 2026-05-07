using Game.Features.Locations;
using UnityEngine;
using VContainer;

namespace InputSystem
{
    public class TapProcessor : BaseProcessor, ITapProcessor
    {
        private ITapInputHandler _handler;

        private readonly Filter<ITapInputHandler> _filter = new();

        private LocationRaycaster _raycaster;
        private ScreenPointConverter _screenPointConverter;

        [Inject]
        public void Install(LocationRaycaster raycaster, ScreenPointConverter screenPointConverter)
        {
            _raycaster = raycaster;
            _screenPointConverter = screenPointConverter;

            _filter.Init();
        }

        public IFilterBuilder AddCommand<TCommand>(TCommand command) where TCommand : ITapInputHandler, IFilterNode
        {
            return _filter.AddCommand(command);
        }

        public void OnPointerDown(Vector2 position)
        {
            var (canvasHit, worldHit) = IsUIPressed(out _);
            if (canvasHit) return;
            
            _handler = _filter.FindHandler(_raycaster.Raycast(position));

            if (_handler == null) return;
            if (worldHit && !_handler.IsOverWorldCanvas)
            {
                _handler = null;
                return; 
            }
            
            _handler?.OnPointerDown();
        }

        public void OnHold(Vector2 position, Vector2 delta) { }

        public void OnPointerUp(Vector2 position)
        {
            if (_handler == null)
                return;

            _handler.InPointerUp();
            var worldPos = _screenPointConverter.ScreenToWorld(position);
            _handler.OnTap(worldPos);
        }

        public void Cancel() 
        { 
            _handler?.OnCancel();
            _handler?.InPointerUp();
        }

        public void Dispose()
        {
            _handler = null;
        }
    }
}
