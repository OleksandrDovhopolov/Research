namespace InputSystem
{
    public class EntryPoint : FilterNode<object, object>
    {
        public override object Validate(object obj, object param = null)
        {
            if (obj == null && param == null)
                return null;

            return MoveNext(obj, param);
        }
    }
}