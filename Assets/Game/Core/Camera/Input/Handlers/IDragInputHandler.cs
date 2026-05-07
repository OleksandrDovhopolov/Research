using UnityEngine;

namespace InputSystem
{
    public interface IDragInputHandler : IInputHandler
    {
        public object TargetObject { get; }
        public void OnDragStart(Vector3 position);
        public void OnHold(Vector3 position, Vector3 delta);
        public void OnDragEnd(Vector3 position);
        public Vector2 HandlePosition(Vector2 position);
    }
}