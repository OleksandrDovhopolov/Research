using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Infrastructure;
using UnityEngine;
using VContainer;

namespace Utils
{
    public abstract class BaseStateMachineArgs
    {
    }

    public class BaseStateMachine : BaseStateMachine<BaseState>
    {
        public BaseStateMachine(IObjectResolver resolver) : base(resolver)
        {
        }
    }

    public class BaseStateMachine<T> : IChangeStateHandler where T : BaseState
    {
        protected readonly HashSet<T> CachedStates = new();

        private IObjectResolver _resolver;
        private T _currentState;
        public T CurrentState => _currentState;

        public BaseStateMachine(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        [Inject]
        public void Install(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public void AddState(T state)
        {
            state.Setup(this);
            _resolver.Inject(state);

            CachedStates.Add(state);
        }

        public virtual void ChangeState<TState>(BaseStateMachineArgs args = null) where TState : BaseState
        {
            _currentState?.OnExit();

            var nextState = CachedStates.FirstOrDefault(s => s is TState);

            if (nextState != null)
            {
                _currentState = nextState;
                _currentState.ApplyArgs(args);
                _currentState.OnEnter();
            }
            else
            {
                Debug.LogError($"Can't find correct state with type '{TypeOf<T>.Raw}'");
            }
        }

        public bool IsStateEquals<TState>() where TState : BaseState
        {
            return _currentState is TState;
        }

        public void Clear()
        {
            _currentState?.OnExit();
            _currentState = null;
            CachedStates.Clear();
        }
    }
    
    public interface IChangeStateHandler
    {
        public void ChangeState<T>(BaseStateMachineArgs args = null) where T : BaseState;
    }
}