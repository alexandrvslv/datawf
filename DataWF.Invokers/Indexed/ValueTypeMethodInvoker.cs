using System.Reflection;

namespace DataWF.Invokers;

public class ValueTypeMethodInvoker<T, V>(MethodInfo infoGet)
    : MethodInvoker<T, V>(infoGet), IValueTypeIndexInvoker<T, V, object[]>
    where T : struct
{ }
