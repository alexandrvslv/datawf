using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace DataWF.Generator.Records;

public record class AttributeRecord(
    TypeRecordReference? ClassRef,
    EquatableList<(string Key, ConstantRecord Value)> NamedArguments,
    EquatableList<ConstantRecord> ConstructorArguments)
{
    public AttributeRecord(AttributeData p, TypeRecordProvider provider)
        : this(
            ClassRef: provider.GetOrCreate(p.AttributeClass),
            NamedArguments: p.NamedArguments.Select(x => (Key: x.Key, Value: new ConstantRecord(x.Value))).ToEquatableList(),
            ConstructorArguments: p.ConstructorArguments.Select(x => new ConstantRecord(x)).ToEquatableList())
    { }

    public AttributeRecord(CustomAttributeData x, TypeRecordProvider provider)
        : this(
            ClassRef: provider.GetOrCreate(x.AttributeType),
            NamedArguments: x.NamedArguments.Select(x => (Key: x.MemberName, Value: new ConstantRecord(x.TypedValue))).ToEquatableList(),
            ConstructorArguments: x.ConstructorArguments.Select(x => new ConstantRecord(x)).ToEquatableList())
    { }

    public TypeRecord? Class => ClassRef?.Type;
}

