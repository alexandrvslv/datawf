namespace DataWF.Common
{
    public interface IListIndexes
    {
        IListIndex Add(IInvoker invoker);
        void AddItem(object item);
        void Clear();
        IListIndex GetIndex(string property);
        void RemoveItem(object item);
    }

    public interface IListIndexes<T> : IListIndexes
    {
        void AddItem(T item);
        void RemoveItem(T item);
    }
}