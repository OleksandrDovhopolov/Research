using CameraModule;
using UnityEngine;

namespace InputSystem
{
    public class ScreenPointConverter
    {
        public static readonly Plane XZPlane = new(new Vector3(-10000, 0, -10000), new Vector3(-10000, 0, 10000), new Vector3(10000, 0, 10000));
        
        private CameraBehaviour _cameraBehaviour;
        
        public void Configurate(CameraBehaviour cameraBehaviour)
        {
            _cameraBehaviour = cameraBehaviour;
        }
        
        public Vector3 ScreenToWorld(Vector3 screenPoint)
        {
            var plane = XZPlane;
            //_cameraBehaviour.Camera.ScreenToWorldPoint(screenPoint);
            var ray = _cameraBehaviour.Camera.ScreenPointToRay(screenPoint);
            return plane.Raycast(ray, out var enterValue) ? ray.GetPoint(enterValue) : Vector3.zero;
        }
    }
}