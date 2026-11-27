using System;

namespace DataWF.Serialization.Elements;

public abstract class PrimitiveSerializer<T> : UnmanagedSerializer<T> where T : unmanaged
{
    public static new SerializeTypeInfo<T> GetTypeInfo()
    {
        return new SerializeTypeInfo<T>(
            keys: SerializeTypeKeys.Attribute,
            typeName: typeof(T).Name,
            shortName: typeof(T).Name,
            constructor: null!,
            properties: Array.Empty<ISerializePropertyInfo<T>>()
        );
    }
    
    public PrimitiveSerializer():base(GetTypeInfo())
    { }
    
    public override bool IsAttributeSerializer => true;
    
}