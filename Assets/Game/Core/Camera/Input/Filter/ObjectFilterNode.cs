namespace InputSystem
{
    public sealed class ObjectFilterNode<T> : FilterNode<object, T> where T : class
    {
        public override object Validate(object obj, object param = null)
        {
            return obj is T target ? MoveNext(target, param) : null;
        }
    }
}
