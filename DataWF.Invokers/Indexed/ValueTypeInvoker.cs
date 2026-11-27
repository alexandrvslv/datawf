namespace DataWF.Invokers;

public class ValueTypeInvoker<T, V, K>(string name, GetIndexHandler<T, K, V> getAction, SetIndexHandler<T, K, V>? setAction = null)
    : ActionIndexInvoker<T, V, K>(name, getAction, setAction), IValueTypeIndexInvoker<T, V, K>
    where T : struct
{ }
