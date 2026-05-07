using System;
using UnityEngine;

namespace InputSystem
{
    public interface ITapProcessor : IInputProcess
    {
    }

    public interface ILongTapProcessor : IInputProcess
    {
    }

    public interface IDragProcessor : IInputProcess
    {
    }
    
    public interface IInputProcess : IDisposable
    {
        public void OnPointerDown(Vector2 position);
        public void OnHold(Vector2 position, Vector2 delta);
        public void OnPointerUp(Vector2 position);
        public void Cancel();
    }
}