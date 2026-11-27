using System.Reflection;

namespace DataWF.Invokers;

public record MemberInvokeInfo(
    MemberInfo Info,
    object? Index
);