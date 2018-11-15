namespace DataWF.Common
{
    public interface IIndexInvoker : IInvoker
    {
        object Index { get; set; }

        object GetValue(object target, object index);

        void SetValue(object target, object index, object value);
    }

    public interface IIndexInvoker<T, V, K> : IIndexInvoker, IInvoker<T, V>
    {
        new K Index { get; set; }

        V GetValue(T target, K index);

        void SetValue(T target, K index, V value);
    }

}
