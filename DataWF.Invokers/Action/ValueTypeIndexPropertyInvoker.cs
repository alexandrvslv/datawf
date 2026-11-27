using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DataWF.Invokers;

public class ValueTypeIndexPropertyInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllProperties
    | DynamicallyAccessedMemberTypes.AllMethods)]
T, V, K>
    : IndexPropertyInvoker<T, V, K>, IValueTypeIndexInvoker<T, V, K>
    where T : struct
{
    public ValueTypeIndexPropertyInvoker(string name) : base(name)
    { }

    public ValueTypeIndexPropertyInvoker(PropertyInfo info, object? index) : base(info, index)
    { }

    public ValueTypeIndexPropertyInvoker(PropertyInfo info, K index) : base(info, index)
    { }
}

