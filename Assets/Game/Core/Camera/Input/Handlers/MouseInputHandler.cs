using UnityEngine;
using VContainer.Unity;

namespace InputSystem
{
    public class MouseInputHandler : InputHandler, ITickable
    {
        private Vector3 _startPosition;
        private Vector3 _lastPosition;
        private bool _isRunning;

        public void Tick()
        {
            if (Input.touchCount >= 2)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                _startPosition = _lastPosition = Input.mousePosition;
                _isRunning = true;

                var touch = new Touch
                {
                    fingerId = 0,
                    position = _startPosition,
                    rawPosition = _startPosition,
                    deltaPosition = Vector2.zero,
                    deltaTime = 0,
                    tapCount = 1,
                    phase = TouchPhase.Began,
                    pressure = 0,
                    maximumPossiblePressure = 0,
                    type = TouchType.Direct,
                    altitudeAngle = 0,
                    azimuthAngle = 0,
                    radius = 0,
                    radiusVariance = 0
                };

                Handle(touch);
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                _isRunning = false;
                var delta = _lastPosition - Input.mousePosition;
                _lastPosition = Input.mousePosition;
                

                var touch = new Touch
                {
                    fingerId = 0,
                    position = _lastPosition,
                    rawPosition = _startPosition,
                    deltaPosition = delta,
                    deltaTime = Time.deltaTime,
                    tapCount = 1,
                    phase = TouchPhase.Ended,
                    pressure = 0,
                    maximumPossiblePressure = 0,
                    type = TouchType.Direct,
                    altitudeAngle = 0,
                    azimuthAngle = 0,
                    radius = 0,
                    radiusVariance = 0
                };

                Handle(touch);
                return;
            }

            if (Input.GetMouseButton(0))
            {
                var delta = _lastPosition - Input.mousePosition;
                _lastPosition = Input.mousePosition;

                var touch = new Touch
                {
                    fingerId = 0,
                    position = _lastPosition,
                    rawPosition = _startPosition,
                    deltaPosition = delta,
                    deltaTime = Time.deltaTime,
                    tapCount = 1,
                    phase = delta == Vector3.zero ? TouchPhase.Stationary : TouchPhase.Moved,
                    pressure = 0,
                    maximumPossiblePressure = 0,
                    type = TouchType.Direct,
                    altitudeAngle = 0,
                    azimuthAngle = 0,
                    radius = 0,
                    radiusVariance = 0
                };

                Handle(touch);
                return;
            }

            if (_isRunning)
            {
                var touch = new Touch
                {
                    fingerId = 0,
                    position = Vector2.zero,
                    rawPosition = Vector2.zero,
                    deltaPosition = Vector2.zero,
                    deltaTime = Time.deltaTime,
                    tapCount = 1,
                    phase = TouchPhase.Canceled,
                    pressure = 0,
                    maximumPossiblePressure = 0,
                    type = TouchType.Direct,
                    altitudeAngle = 0,
                    azimuthAngle = 0,
                    radius = 0,
                    radiusVariance = 0
                };

                _isRunning = false;
                Handle(touch);
            }
        }
    }
}