using Microsoft.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace DataWF.Generator.Records;

public record class ConstructorRecord(
    EquatableList<ParameterRecord> Parameters,
    Accessibility Accessibility)
{
    public ConstructorRecord(IMethodSymbol x, TypeRecordProvider provider)
        : this(
            Parameters: x.Parameters.Select(x => new ParameterRecord(x, provider)).ToEquatableList(),
            Accessibility: x.DeclaredAccessibility)
    { }

    public ConstructorRecord(ConstructorInfo x, TypeRecordProvider provider)
        : this(
            Parameters: x.GetParameters().Select(x => new ParameterRecord(x, provider)).ToEquatableList(),
            Accessibility: x.IsPublic ? Accessibility.Public : x.IsPrivate ? Accessibility.Private : Accessibility.Internal)
    { }
}
