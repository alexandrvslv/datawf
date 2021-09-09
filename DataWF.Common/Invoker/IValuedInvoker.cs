namespace DataWF.Common
{
    public interface IValuedInvoker<V> : IInvoker
    {
        new V GetValue(object target);
        void SetValue(object target, V value);
    }
}
