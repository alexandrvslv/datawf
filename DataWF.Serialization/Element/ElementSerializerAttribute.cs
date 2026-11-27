using System;
using DataWF.Invokers;

namespace DataWF.Serialization;

[AttributeUsage(AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Property
    | AttributeTargets.Field
    | AttributeTargets.Interface)]
public class ElementSerializerAttribute : InvokerAttribute
{
    public Type? SerializerType { get; set; }

    public int Order { get; set; }
}
