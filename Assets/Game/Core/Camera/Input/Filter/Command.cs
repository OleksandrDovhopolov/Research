namespace InputSystem
{
    public abstract class Command<TIn> : FilterNode<TIn>
    {
        protected TIn Target;
        
        public sealed override bool TrySetNext(IFilterNode next, bool asDefault) => false;

        public sealed override object Validate(TIn obj, object param = null)
        {
            if (!IsValid(obj, param))
                return null;

            Target = obj;
            return this;
        }

        protected abstract bool IsValid(TIn obj, object param = null);
    }

    public abstract class Command : IFilterNode
    {
        public bool TrySetNext(IFilterNode next, bool asDefault = false) => false;

        public T GetNode<T>() => default;
    }
}