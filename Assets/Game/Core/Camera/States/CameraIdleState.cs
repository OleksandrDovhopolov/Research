using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using InputSystem;
using UnityEngine;

namespace CameraModule
{
    public class CameraIdleState : BaseCameraState
    {
        const int TouchQueueSize = 2;
        Queue<float> _touchQueue = new Queue<float>();

        public CameraIdleState(CameraBehaviour cameraBehaviour, CameraSettings cameraSettings) : base(cameraBehaviour, cameraSettings)
        {
        }

        public override void OnEnter()
        {
            InputHandler.OnInputEvent += OnInput;
            base.OnEnter();
        }
        
        public override void OnExit()
        {
            InputHandler.OnInputEvent -= OnInput;
            base.OnExit();
        }

        public override void LateUpdate()
        {
            //if (_cameraBehaviour.IsCameraMovementUnavailable()) return;
            _cameraBehaviour.MoveDelta = Vector3.Lerp(Vector3.zero, _cameraBehaviour.MoveDelta, _cameraSettings.StopInertia);
            _cameraBehaviour.transform.position += _cameraBehaviour.MoveDelta;
        }
        
        
        private void OnInput(Touch touch)
        {
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                _touchQueue.Enqueue(Time.realtimeSinceStartup);
                if (_touchQueue.Count > TouchQueueSize)
                    _touchQueue.Dequeue();
            }
            if (touch.phase != TouchPhase.Began)
                return;
            _cameraBehaviour.ResetMoveData();
        }

        public override void OnDragStart()
        {
            //if (_cameraBehaviour.IsCameraMovementUnavailable()) return;

            _cameraBehaviour.transform.DOKill();
            _cameraBehaviour.ResetMoveData();
            var fingers = _cameraBehaviour.FingerFilter.UpdateAndGetFingers();
            if (fingers.Count == 2)
            {
                ChangeState<CameraPinchZoomState>();
                return;
            }
            if (fingers.Count == 1)
            {
                if (_touchQueue.Count > 0 && Time.realtimeSinceStartup - _touchQueue.Last() - fingers[0].Age < 0.2f)
                {
                    ChangeState<CameraOneHandZoomState>();
                    return;
                }
                ChangeState<CameraDragState>();
            }
        }
    }
}