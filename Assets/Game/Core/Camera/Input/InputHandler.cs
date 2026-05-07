using System;
using System.Collections.Generic;
using UnityEngine;

namespace InputSystem
{
    public interface IInput
    {
        public IDragProcessor DragProcessorHandler {get; set;}
        public ILongTapProcessor LongTapProcessorHandler {get; set;}
        public ITapProcessor TapProcessorHandler {get; set;}
        public IDisposable Block();
    }
    
    public abstract class InputHandler : IInput
    {
        
        public static event Action<Touch> OnInputEvent;
        
        private float _longTapThreshold = 0.4f;
        private float _dragThreshold = 10f;

        public IDragProcessor DragProcessorHandler { get; set;}
        public ILongTapProcessor LongTapProcessorHandler { get; set;}
        public ITapProcessor TapProcessorHandler { get; set;}

        private IInputProcess _current;
        private float _inputTime;
        private Vector2 _startTouchPosition;
        private readonly List<IDisposable> _lockers = new();

        public IDisposable Block()
        {
            var block = new DisposableSource();
            block.DisposeAction += () => _lockers.Remove(block);
            _current?.Cancel();
            _current?.Dispose();
            _current = null;
            _lockers.Add(block);
            
            return block;
        }

        protected void Handle(Touch touch)
        {
            try
            {
                if (_lockers.Count > 0)
                    return;

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        _inputTime = Time.realtimeSinceStartup;
                        _startTouchPosition = touch.position;

                        _current?.Cancel();
                        _current?.Dispose();
                        
                        _current = TapProcessorHandler;
                        _current.OnPointerDown(_startTouchPosition);
                        break;
                    case TouchPhase.Moved:
                        var moveDistance = Vector3.Distance(touch.position, _startTouchPosition);
                        if (_current != DragProcessorHandler && moveDistance > _dragThreshold)
                        {
                            _current?.Cancel();
                            _current?.Dispose();
                            
                            _current = DragProcessorHandler;
                            _current?.OnPointerDown(_startTouchPosition);
                        }

                        _current?.OnHold(touch.position, touch.deltaPosition);
                        break;
                    case TouchPhase.Stationary:
                        if (_current == TapProcessorHandler && Time.realtimeSinceStartup - _inputTime > _longTapThreshold)
                        {
                            _current?.Cancel();
                            _current?.Dispose();
                            _current = LongTapProcessorHandler;
                            _current?.OnPointerDown(touch.position);
                        }

                        _current?.OnHold(touch.position, touch.deltaPosition);
                        break;
                    case TouchPhase.Ended:
                        _current?.OnPointerUp(touch.position);
                        _current?.Dispose();
                        _current = null;
                        break;
                    case TouchPhase.Canceled:
                        _current?.Cancel();
                        _current?.Dispose();
                        _current = null;
                        break;
                }
                
                OnInputEvent?.Invoke(touch);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    } 
    
    public class DisposableSource : IDisposable
    {
        public Action DisposeAction;

        public DisposableSource() { }

        public DisposableSource(Action disposeAction)
        {
            DisposeAction = disposeAction;
        }

        public void Dispose()
        {
            DisposeAction();
        }
    }
}

