using System;

namespace Utils
{
    public abstract class BaseState
    {
        private Action<Type, BaseStateMachineArgs> _onChangeStateHandler;
        private IChangeStateHandler _changeStateHandler;

        public abstract void OnEnter();
        public abstract void OnExit();

        public virtual void ApplyArgs(BaseStateMachineArgs args = null)
        {
        }

        public void Setup(IChangeStateHandler changeStateHandler)
        {
            _changeStateHandler = changeStateHandler;
        }

        protected virtual void ChangeState<T>(BaseStateMachineArgs args = null) where T : BaseState
        {
            _changeStateHandler.ChangeState<T>(args);
        }
    }
}