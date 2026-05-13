using System.Collections;
using System.Collections.Generic;
using CameraModule;
using UnityEngine;
using VContainer;

namespace InputSystem
{
    public class CameraDragCommand : IDragInputHandler
    {
        private CameraBehaviour _cameraBehaviour;

        [Inject]
        public void Install(CameraBehaviour cameraBehaviour)
        {
            _cameraBehaviour = cameraBehaviour;
        }

        public object TargetObject => null;

        public void OnDragStart(Vector3 position)
        {
            _cameraBehaviour.DragStart();
        }

        public void OnHold(Vector3 position, Vector3 delta)
        {
            _cameraBehaviour.Drag();
        }

        public void OnDragEnd(Vector3 position)
        {
            _cameraBehaviour.DragEnd();
        }

        public Vector2 HandlePosition(Vector2 position)
        {
            return position;
        }

        public void OnCancel()
        {
            _cameraBehaviour.DragEnd();
        }
    }
}