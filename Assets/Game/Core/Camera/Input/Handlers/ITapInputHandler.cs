using UnityEngine;

namespace InputSystem
{
    public interface ITapInputHandler : IInputHandler
    {
        public void OnTap(Vector3 position);
        public void OnPointerDown();
        public void InPointerUp();

        public bool IsOverWorldCanvas { get; }
    }
}