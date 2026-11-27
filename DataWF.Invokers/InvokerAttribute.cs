using System;

namespace DataWF.Invokers;

[AttributeUsage(AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Property
    | AttributeTargets.Method
    | AttributeTargets.Field
    | AttributeTargets.Constructor
    | AttributeTargets.Interface)]
public class InvokerAttribute : Attribute
{
    public bool Instance { get; set; } = true;
    public bool Ignore { get; set; }
    public Type? ToGenerate { get; set; }
}
