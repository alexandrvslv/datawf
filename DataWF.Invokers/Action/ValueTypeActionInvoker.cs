namespace DataWF.Invokers;

public class ValueTypeActionInvoker<T, V>(string name, GetHandler<T, V> getAction, SetHandler<T, V>? setAction = null)
    : ActionInvoker<T, V>(name, getAction, setAction), IValueTypeInvoker<T, V>
 where T : struct
{ }

