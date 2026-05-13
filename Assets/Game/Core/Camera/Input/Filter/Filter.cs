using System;
using System.Collections.Generic;
using UnityEngine;

namespace InputSystem
{
    public interface IFilterBuilder
    {
        IFilterBuilder WithFilter<TFilter>() where TFilter : IFilterNode, new();
        IFilterBuilder WithFilter<TFilter>(Func<TFilter> filterFactory) where TFilter : IFilterNode, new();
        public void Build(bool asDefault = false);
    }
    
    public class Filter<T>where T : class
    {
        private EntryPoint _entryPoint;
        private T _defaultHandler;

        public void Init(T defaultHandler = default)
        {
            _entryPoint = new EntryPoint();
            _defaultHandler = defaultHandler;
        }

        public IFilterBuilder AddCommand<TCommand>(TCommand command) where TCommand : T, IFilterNode
        {
            return new FilterBuilder { Parent = _entryPoint, Command = command};
        }

        public T FindHandler(List<object> objects, object param = null)
        {
            foreach (var obj in objects)
            {
                var handler = FindHandler(obj, param);
                
                if (handler != null)
                    return handler;
            }

            return _defaultHandler;
        }
        
        private T FindHandler(object obj, object param = null) 
        {
            return _entryPoint.Validate(obj, param) as T;
        }
        
        private class FilterBuilder : IFilterBuilder
        {
            public IFilterNode Parent;
            public IFilterNode Command;
            
            public IFilterBuilder WithFilter<TFilter>() where TFilter : IFilterNode, new()
            {
                var node = Parent.GetNode<TFilter>();
                if (node == null)
                {
                    node = new TFilter();

                    if (!Parent.TrySetNext(node, false))
                    {
                        Debug.LogError($"Cant append node {typeof(TFilter)} to node {Parent.GetType()}");
                        return null;
                    }
                }
                
                return new FilterBuilder { Parent = node, Command = Command};
            }
            
            public IFilterBuilder WithFilter<TFilter>(Func<TFilter> filterFactory) where TFilter : IFilterNode, new()
            {
                var node = Parent.GetNode<TFilter>();
                if (node == null)
                {
                    node = filterFactory();

                    if (!Parent.TrySetNext(node, false))
                    {
                        Debug.LogError($"Cant append node {typeof(TFilter)} to node {Parent.GetType()}");
                        return null;
                    }
                }
                
                return new FilterBuilder { Parent = node, Command = Command};
            }

            public void Build(bool asDefault = false)
            {
               Parent.TrySetNext(Command, asDefault);
            }
        }
    }
}