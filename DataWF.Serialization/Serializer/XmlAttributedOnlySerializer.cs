using System;
using System.Collections.Generic;
using System.Linq;
using DataWF.Serialization.Elements;

namespace DataWF.Serialization;

public class XmlAttributedOnlySerializer : XmlInvokerSerializer
{
    public override IElementSerializer<T> GetSerializer<T>()
    {
        if (serializers.Count > 0 && serializers.TryGetValue(typeof(T), out var serializer))
            return (IElementSerializer<T>)serializer;
        var baseSerializer = base.GetSerializer<T>();
        if(baseSerializer is TypeInfoSerializer<T>)
        {
            var baseTypeInfo = baseSerializer.TypeInfo;
            var newSerializer = baseSerializer.WithTypeInfo(new SerializeTypeInfo<T>(
            typeName: baseTypeInfo.TypeName,
            keys: baseTypeInfo.Keys,
            constructor: baseTypeInfo.Constructor,
            properties: [.. baseTypeInfo.Properties.Where(p => p.IsXmlAttributed)]));
            serializers[typeof(T)] = newSerializer;
            return newSerializer;
        }
        return baseSerializer;
    }

    
}

