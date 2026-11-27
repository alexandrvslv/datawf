using System.Reflection;
using Microsoft.CodeAnalysis;

namespace DataWF.Generator.Records;

public record struct ConstantRecord(bool IsNull = true, object? Value = null)
{
    public ConstantRecord(TypedConstant value)
        : this(value.IsNull, value.Kind == TypedConstantKind.Array ? value.Values : value.Value)
    { }

    public ConstantRecord(CustomAttributeTypedArgument x) 
        : this(x.Value == null, x.Value)
    { }
}

