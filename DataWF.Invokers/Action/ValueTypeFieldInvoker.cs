using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DataWF.Invokers;

public class ValueTypeFieldInvoker<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.AllFields)] T, V>
    : FieldInvoker<T, V>, IValueTypeInvoker<T, V>
    where T : struct
{
    public ValueTypeFieldInvoker(string fieldName) : base(fieldName)
    { }

    public ValueTypeFieldInvoker(FieldInfo info) : base(info)
    { }
}
