using Microsoft.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace DataWF.Generator.Records;
public record MethodRecord(string Name, 
    TypeRecordReference? TypeRef, 
    bool IsStatic, 
    bool ReturnsVoid,
    EquatableList<ParameterRecord>? Parameters, 
    EquatableList<AttributeRecord>? Attributes = null)
    : AttributedRecord(Name, TypeRef, Attributes)
{
    public MethodRecord(IMethodSymbol p, TypeRecordProvider provider)
        : this(
            Name: p.Name, 
            TypeRef: provider.GetOrCreate(p.ReturnType), 
            IsStatic: p.IsStatic, 
            ReturnsVoid: p.ReturnsVoid,
            Parameters: p.Parameters.Select(x => new ParameterRecord(x, provider)).ToEquatableList(), 
            Attributes: p.GetAttributes().Select(x => new AttributeRecord(x, provider)).ToEquatableList())
    { }

    public MethodRecord(MethodInfo x, TypeRecordProvider provider)
        : this(
            Name: x.Name, 
            TypeRef: provider.GetOrCreate(x.ReturnType), 
            IsStatic: x.IsStatic, 
            ReturnsVoid: x.ReturnType == typeof(void),
            Parameters: x.GetParameters().Select(x => new ParameterRecord(x, provider)).ToEquatableList(), 
            Attributes: x.GetCustomAttributesData().Select(x => new AttributeRecord(x, provider)).ToEquatableList())
    { }

    public TypeRecord ReturnType => base.Type!;
}
