using UnityEngine;
using VContainer.Unity;

namespace InputSystem
{
    public class TouchInputHandler : InputHandler, ITickable
    {
        private int _curFingerId = -1;

        public void Tick()
        {
            if (Input.touchCount == 0)
            {
                if (_curFingerId < 0)
                    return;

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

                _curFingerId = -1;
                Handle(touch);
                return;
            }

            if (_curFingerId < 0)
            {
                var touch = Input.GetTouch(0);
                _curFingerId = touch.fingerId;
                Handle(touch);
                return;
            }

            foreach (var touch in Input.touches)
            {
                if (touch.fingerId != _curFingerId)
                    continue;

                Handle(touch);

                if (touch.phase is TouchPhase.Ended or TouchPhase.Canceled)
                    _curFingerId = -1;

                return;
            }
        }
    }
}