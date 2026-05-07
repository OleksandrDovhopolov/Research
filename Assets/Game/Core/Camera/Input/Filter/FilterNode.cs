using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InputSystem
{
    public interface IFilterNode
    {
        public bool TrySetNext(IFilterNode next, bool asDefault);
        public T GetNode<T>();
    }
    
    public interface IFilterNode<in T> : IFilterNode
    {
        public object Validate(T obj, object param = null);
    }
    
    public abstract class FilterNode<TIn> : IFilterNode<TIn>
    {
        public abstract bool TrySetNext(IFilterNode next, bool asDefault);

        public abstract object Validate(TIn obj, object param = null);

        public virtual T GetNode<T>()
        {
            return default;
        }
    }

    public abstract class FilterNode<TIn, TOut> : FilterNode<TIn>
    {
        private List<IFilterNode<TOut>> _next = new();
        private IFilterNode _defaultCommand;

        public sealed override bool TrySetNext(IFilterNode next, bool asDefault)
        {
            if (asDefault)
            {
                if (_defaultCommand != null)
                    Debug.LogWarning($"{next.GetType()} hide default command {_defaultCommand.GetType()}");

                _defaultCommand = next;
                return true;
            }
            
            if (next is IFilterNode<TOut> result)
            {
                if (_next.Contains(next))
                    return false;
                
                _next.Add(result);
                _next = _next.OrderByDescending(x => x is Command<TOut>).ToList();
                return true;
            }

            return false;
        }

        protected object MoveNext(TOut obj, object param)
        {
            foreach (var filter in _next)
            {
                var result = filter.Validate(obj, param);
                if (result != null)
                {
                    return result;
                }
            }

            return _defaultCommand;
        }

        public override T GetNode<T>()
        {
            return _next.OfType<T>().FirstOrDefault();
        }
    }
}