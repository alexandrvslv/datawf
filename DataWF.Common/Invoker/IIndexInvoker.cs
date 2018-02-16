namespace DataWF.Common
{
    public interface IIndexInvoker : IInvoker
    {
        object Index { get; set; }

        object Get(object target, object index);

        void Set(object target, object index, object value);
    }

    public interface IIndexInvoker<T, V, K> : IIndexInvoker, IInvoker<T, V>
    {
        new K Index { get; set; }

        V Get(T target, K index);

        void Set(T target, K index, V value);
    }

}
