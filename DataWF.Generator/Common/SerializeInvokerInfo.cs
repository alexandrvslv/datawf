namespace DataWF.Generator;

internal record SerializeInvokerInfo(InvokerInfo Invoker,
    bool NonSerialize,
    string FieldName,
    string FieldType,
    string LeftArg,
    string RightArg,
    string Default,
    int Order,
    bool IsInitOnly,
    string? CtorParam = null)
{
}