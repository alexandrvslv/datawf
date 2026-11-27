using DataWF.Generator.Records;

namespace DataWF.Generator;

internal record struct InvokerInfo(string Name, 
    TypeRecord DeclareType,
    bool HasInstance,
    AttributedRecord Member,
    bool Exist = false)
{
    public TypeRecord MemberType => Member.TypeRef?.Type!;
    public string MemberName => Member.Name ?? string.Empty;

    public bool IsGeneric => DeclareType.IsGenericInvoker || Member is PropertyRecord property && property.IsOverride;
    public bool Inherit { get; internal set; }
}


