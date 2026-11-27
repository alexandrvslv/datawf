using DataWF.Generator.Records;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DataWF.Generator;

internal class SerializerGenerator : InvokerGenerator
{
    public SerializerGenerator(TypeRecordProvider provider, TypeRecord type)
        : base(provider, type)
    { }

    public SerializerGenerator(TypeRecordProvider provider, INamedTypeSymbol typeSymbol)
        : base(provider, typeSymbol)
    { }

    public override string GetFileName() => GetSerializerFileName();

    public string GetSerializerFileName() => $"{Namespace}.{Type.GetGeneratedTypeName()}{Type.GenericArgsForFileName}Serializer.g.cs";

    private string GetSerializerName(TypeRecord type) =>
        $"{type.OriginName}Serializer{(IsNestedType ? string.Empty : type.GenericArgs)}";

    public override bool ProcessInternal(ref SourceProductionContext context)
    {
        if (!base.ProcessInternal(ref context))
            return false;

        var source = GenerateSerializer();
        if (source != null)
        {
            context.AddSource(GetSerializerFileName(), source);
            return true;
        }

        return false;
    }

    public string? GenerateSerializer()
    {
        if (newInvokers.Count == 0
            && ctors.Count == 0
            && existInvokers.Count == 0)
        {
            return null;
        }

        usings.Add("DataWF.Serialization");
        usings.Add("DataWF.Serialization.Elements");
        usings.Add("MemoryPack");
        usings.Add("MessagePack");
        usings.Add("System.Text.Json");
        usings.Add("System.Text.Json.Serialization");
        usings.Remove(Type.Namespace);
        source.Clear();
        source.Append($$"""
        #nullable enable

        {{string.Join("\n", usings.OrderBy(u => u, StringComparer.Ordinal)
            .Select(u => $"using {u};"))}}

        namespace {{Namespace}};
        """);
        if (IsNestedType)
        {
            source.Append($$"""

            {{Type.GetContainingTypeOpeners()}}
            {{Type.GetTypeDeclaration("ISerializerRegister")}}
            {
            """);
        }
        foreach (var item in typeInvokers)
        {
            if (item.ToGenerate)
                GenerateSerializer(item);
        }
        if (!IsNestedType)
        {
            source.Append($$"""

            {{Type.GetTypeDeclaration("ISerializerRegister")}}
            {
            """);
        }
        Register();
        source.Append($$"""

        }
        {{(IsNestedType ? "\n" + Type.GetContainingTypeClosers() : string.Empty)}}
        """);
        return source.ToString();
    }

    private void GenerateSerializer(TypeInvokerInfo typeInfo)
    {
        var type = typeInfo.Type;
        var serializerName = GetSerializerName(type);
        var serializeProperties = new List<SerializeInvokerInfo>(typeInfo.Invokers.Count);
        var constructor = GetConstructor(type);
        var index = 0;
        source.Append($$"""

        public class {{serializerName}} : TypeInfoSerializer<{{type}}>
        {{(type.IsGenericType ? string.Join("    \n", type.GenericParameters
            .Where(x => !string.IsNullOrEmpty(x.GenericParameter))
            .Select(x => $"where {x.Name} : {x.GenericParameter}"))
            : string.Empty)}}
        {
            public static readonly {{serializerName}} Instance = new();
            public static new SerializeTypeInfo<{{type}}> GetTypeInfo()
            {
                return new SerializeTypeInfo<{{Type}}>(
                    typeName: {{(Type.IsGenericType ? "null" : $"nameof({Type})")}},
                    keys: SerializeTypeKeys.None,
                    constructor: {{FormatConstructor(constructor)}},
                    properties: [
        """);
        foreach (var invokerInfo in typeInfo.Invokers)
        {
            AppendPropertyInfo(invokerInfo);
        }
        source.Append($$"""
                
                    ]
                );
            }
        """);
        serializeProperties.Sort((x, y) => x.Order.CompareTo(y.Order));
        foreach (var serializeInfo in serializeProperties)
        {
            source.Append($$"""

                private {{serializeInfo.FieldType}} {{serializeInfo.FieldName}};
            """);
        }
        source.Append($$"""

            public {{type.OriginName}}Serializer()
                : base(GetTypeInfo())
            {
        """);
        for (int i = 0; i < serializeProperties.Count; i++)
        {
            var serializeInfo = serializeProperties[i];
            source.Append($$"""

                {{serializeInfo.FieldName}} = ({{serializeInfo.FieldType}})typeInfo.SerializeProperties[{{i}}];
        """);
        }
        source.Append($$"""

            }
        """);
        var hasInitOnly = serializeProperties.Any(x => x.IsInitOnly);
        GenerateMemoryPack(type, serializeProperties, constructor, hasInitOnly);
        GenerateMessagePack(type, serializeProperties, constructor, hasInitOnly);
        GenerateJson(type, serializeProperties, constructor, hasInitOnly);
        GenerateXML(type, serializeProperties, constructor, hasInitOnly);

        source.Append($$"""

        }
        """);

        void AppendPropertyInfo(InvokerInfo invoker)
        {
            var order = GetOrder(invoker);
            var serializeInfo = new SerializeInvokerInfo(
                Invoker: invoker,
                NonSerialize: IsNonSerialize(invoker),
                IsInitOnly: IsInitOnly(invoker.Member!),
                FieldName: $"_{invoker.MemberName}Info",
                FieldType: $"ISerializePropertyInfo<{type}, {invoker.MemberType}>",
                LeftArg: $"_{invoker.MemberName}",
                RightArg: $"_{invoker.MemberName}{(invoker.MemberType.IsValueType ? string.Empty : "!")}",
                Default: GetDefault(invoker),
                Order: order ?? index,
                CtorParam: MapConstructorParam(constructor, invoker)
            );
            index++;
            if (!serializeInfo.NonSerialize)
                serializeProperties.Add(serializeInfo);
            source.Append($$"""

                        new {{serializeInfo.FieldType.Substring(1)}}(
                            invoker: {{$"{invoker.DeclareType}.{invoker.Name}{(invoker.IsGeneric ? $"<{type}>" : string.Empty)}"}}.Instance,
                            keys: {{GetKeys(invoker, serializeInfo.NonSerialize)}},
                            defaultValue: {{serializeInfo.Default}},
                            serializer: {{GetSerializer(invoker)}},
                            order: {{serializeInfo.Order}}
                        ),
        """);
        }
    }

    private string? MapConstructorParam(CtorInfo ctorInfo, InvokerInfo invoker)
    {
        var memberName = invoker.MemberName;
        foreach (var param in ctorInfo.Constructor.Parameters)
        {
            if (string.Equals(param.Name, memberName, StringComparison.OrdinalIgnoreCase))
            {
                return param.Name;
            }
        }
        return null;
    }

    private void GenerateJson(TypeRecord type, List<SerializeInvokerInfo> serializeProperties, CtorInfo constructor, bool hasInitOnly)
    {
        source.Append($$"""

            public override void Write(Utf8JsonWriter writer, scoped ref {{type}}{{(type.IsValueType ? string.Empty : "?")}} value, JsonSerializerOptions options)
            {
        """);
        if (!type.IsValueType)
        {
            source.Append($$"""

                if (value == null)
                {
                    writer.WriteNullValue();
                    return;
                }
        """);
        }
        source.Append($$"""

                writer.WriteStartObject();
        """);
        foreach (var property in serializeProperties)
        {
            if (property.Invoker.MemberType == TypeRecord.Byte
              || property.Invoker.MemberType == TypeRecord.SByte
              || property.Invoker.MemberType == TypeRecord.Int16
              || property.Invoker.MemberType == TypeRecord.Int32
              || property.Invoker.MemberType == TypeRecord.Int64
              || property.Invoker.MemberType == TypeRecord.UInt16
              || property.Invoker.MemberType == TypeRecord.UInt32
              || property.Invoker.MemberType == TypeRecord.UInt64
              || property.Invoker.MemberType == TypeRecord.Single
              || property.Invoker.MemberType == TypeRecord.Double
              || property.Invoker.MemberType == TypeRecord.Decimal)
            {
                source.Append($$"""

                writer.WriteNumber({{property.FieldName}}.JsonName, value.{{property.Invoker.MemberName}});
        """);
            }
            else
            {
                source.Append($$"""

                {{property.FieldName}}.Write(writer, ref value, options);
        """);
            }
        }
        source.Append($$"""

                writer.WriteEndObject();
            }

            public override void Read(ref Utf8JsonReader reader, scoped ref {{type}}{{(type.IsValueType ? string.Empty : "?")}} value, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    value = default;
                    return;
                }

                var lookup = TypeInfo.GetLookup();
                var propertyIndex = 0;
                var properties = TypeInfo.SerializeProperties;
        """);
        foreach (var property in serializeProperties)
        {
            source.Append(type.IsValueType
                ? $$"""

                    {{property.Invoker.MemberType}} {{property.LeftArg}} = value.{{property.Invoker.MemberName}};
            """
                : $$"""

                    {{property.Invoker.MemberType}} {{property.LeftArg}} = default{{(
                        property.Invoker.MemberType.IsValueType ? string.Empty : "!")}};
            """);
        }
        if (!type.IsValueType)
        {
            source.Append($$"""
        
                if (value != null)
                {
        """);
            foreach (var property in serializeProperties)
            {
                source.Append($$"""

                    {{property.LeftArg}} = value.{{property.Invoker.MemberName}};        
        """);
            }
            source.Append($$"""
               
                }
        """);
        }

        source.Append($$"""

                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        reader.Skip();
                        continue;
                    }

                    var sequentialPropertyIndex = propertyIndex;
                    var propertyInfo = ReadProperty(ref reader, properties, ref lookup, ref propertyIndex);
                    reader.Read();

                    if (propertyInfo == null)
                    {
                        reader.TrySkip();
                        continue;
                    }
                    if (propertyIndex != sequentialPropertyIndex)
                    {
                        switch (sequentialPropertyIndex)
                        {
        """);
        for (var i = 0; i < serializeProperties.Count; i++)
        {
            var property = serializeProperties[i];
            source.Append($$"""

                            case {{i}}:
                                {{property.FieldName}}.Serializer.Read(ref reader, ref {{property.RightArg}}, options);
                                continue;
            """);
        }
        source.Append($$"""

                        }
                    }
        """);
        foreach (var property in serializeProperties)
        {
            source.Append($$"""

                    if (ReferenceEquals(propertyInfo, {{property.FieldName}}))
                    {
                        {{property.FieldName}}.Serializer.Read(ref reader, ref {{property.RightArg}}, options);
                        continue;
                    }
        """);
        }
        source.Append($$"""

                    reader.TrySkip();
                }
        """);
        AppendConstructor(type, serializeProperties, constructor, hasInitOnly);
        source.Append($$"""
        
            }
        """);
    }

    private void GenerateXML(
        TypeRecord type,
        List<SerializeInvokerInfo> serializeProperties,
        CtorInfo constructor,
        bool hasInitOnly)
    {
        source.Append($$"""

            public override void Write(XmlInvokeWriter writer, ref {{type}}{{(type.IsValueType ? string.Empty : "?")}} value)
            {
        """);
        if (!type.IsValueType)
        {
            source.Append($$"""

                if (value == null)
                {
                    writer.WriteNullValue();
                    return;
                }
        """);
        }

        source.Append($$"""

                base.Write(writer, ref value);
        """);

        source.Append($$"""

            }
        """);
        
        source.Append($$"""

            public override void Read(XmlInvokeReader reader, ref {{type}}{{(type.IsValueType ? string.Empty : "?")}} value)
            {
        """);
        foreach (var property in serializeProperties)
        {
            source.Append(type.IsValueType
                ? $$"""

                    {{property.Invoker.MemberType}} {{property.LeftArg}} = value.{{property.Invoker.MemberName}};
            """
                : $$"""

                    {{property.Invoker.MemberType}} {{property.LeftArg}} = default{{(
                        property.Invoker.MemberType.IsValueType ? string.Empty : "!")}};
            """);
        }
        if (!type.IsValueType)
        {
            source.Append($$"""

                if (value != null)
                {
        """);
            foreach (var property in serializeProperties)
            {
                source.Append($$"""

                    {{property.LeftArg}} = value.{{property.Invoker.MemberName}};
            """);
            }
            source.Append("""

                }
            """);
        }

        source.Append($$"""

                if (reader.HasAttributes)
                {
                    while (reader.MoveToNextAttribute())
                    {
                        if (reader.Name == "nil")
                        {
                            if (reader.ReadContentAsBoolean())
                            {
                                value = default;
                                reader.MoveToElement();
                                return;
                            }
                        }
                        else
                        {
                            switch (reader.Name)
                            {
        """);
        foreach (var property in serializeProperties)
        {
            source.Append($$"""

                                case "{{property.Invoker.MemberName}}":
                                    {{property.FieldName}}.Serializer.Read(reader, ref {{property.RightArg}});
                                    break;
            """);
        }
        source.Append($$"""

                            }
                        }
                    }

                    reader.MoveToElement();
                }
                
                if (!reader.IsEmptyElement)
                {
                    while (reader.ReadNextElement())
                    {
                        switch (reader.Name)
                        {
        """);
        foreach (var property in serializeProperties)
        {
            source.Append($$"""

                            case "{{property.Invoker.MemberName}}":
                                {{property.FieldName}}.Serializer.Read(reader, ref {{property.RightArg}});
                                break;
            """);
        }
        source.Append($$"""

                            default:
                                reader.ReadContent();
                                break;
                        }
                    }
                }
        """);
        AppendConstructor(type, serializeProperties, constructor, hasInitOnly);
        source.Append($$"""

            }
        """);
    }

    private void GenerateMemoryPack(TypeRecord type, List<SerializeInvokerInfo> serializeProperties, CtorInfo constructor, bool hasInitOnly)
    {
        if (type.IsUnmanaged)
        {
            source.Append($$"""

            public override void Read(ref MemoryPackReader reader, scoped ref {{type}} value)
            {
                reader.ReadUnmanaged(out value);
            }

            public override void Write<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref {{type}} value)
            {
                writer.WriteUnmanaged(value);
            }   
        """);
            return;
        }
        source.Append($$"""

            public override void Write<TBuffer>(ref MemoryPackWriter<TBuffer> writer, scoped ref {{type}}{{(type.IsValueType ? string.Empty : "?")}} value)
            {
        """);
        if (!type.IsValueType)
        {
            source.Append($$"""

                if (value == null)
                {
                    writer.WriteNullObjectHeader();
                    return;
                }
        """);
        }
        source.Append($$"""

                writer.WriteObjectHeader({{serializeProperties.Count}});
        """);
        const int maxUnmanagedBatchSize = 15;
        var unmanagedBatch = new List<SerializeInvokerInfo>(maxUnmanagedBatchSize);
        foreach (var property in serializeProperties)
        {
            if (property.Invoker.MemberType.IsUnmanaged)
            {
                unmanagedBatch.Add(property);
                if (unmanagedBatch.Count == maxUnmanagedBatchSize)
                    AppendWriteUnmanagedBatch();

            }
            else if (property.Invoker.MemberType.IsArray
                && property.Invoker.MemberType.OriginType?.IsUnmanaged is true)
            {
                AppendWriteUnmanagedBatch();
                source.Append($$"""

                writer.WriteUnmanagedArray(value.{{property.Invoker.MemberName}});
        """);
            }
            else if (property.Invoker.MemberType == TypeRecord.String
              || property.Invoker.MemberType.OriginType == TypeRecord.String)
            {
                AppendWriteUnmanagedBatch();
                source.Append($$"""

                writer.WriteString(value.{{property.Invoker.MemberName}});
        """);
            }
            else
            {
                AppendWriteUnmanagedBatch();
                source.Append($$"""

                {{property.FieldName}}.Write(ref writer, ref value);
        """);
            }
        }
        AppendWriteUnmanagedBatch();

        source.Append($$"""
        
            }

            public override void Read(ref MemoryPackReader reader, scoped ref {{type}}{{(type.IsValueType ? string.Empty : "?")}} value)
            {
        """);
        source.Append($$"""

                if (!reader.TryReadObjectHeader(out var count))
                {
                    {{(type.IsValueType ? string.Empty : "value = null!;")}}
                    return;
                }
        """);

        foreach (var property in serializeProperties)
        {
            if (type.IsValueType)
            {
                source.Append($$"""

                {{property.Invoker.MemberType}} {{property.LeftArg}} = value.{{property.Invoker.MemberName}};
        """);
            }
            else
            {
                source.Append($$"""

                {{property.Invoker.MemberType}} {{property.LeftArg}} = default{{(
                        property.Invoker.MemberType.IsValueType ? string.Empty : "!")}};
        """);
            }
        }

        if (!type.IsValueType)
        {
            source.Append($$"""
        
                if (value != null)
                {
        """);
            foreach (var property in serializeProperties)
            {
                if (!property.Invoker.MemberType.IsUnmanaged)
                {
                    source.Append($$"""

                    {{property.LeftArg}} = value.{{property.Invoker.MemberName}};        
        """);
                }
            }
            source.Append($$"""

                    if (count < {{serializeProperties.Count}})
                    {
        """);
            foreach (var property in serializeProperties)
            {
                if (property.Invoker.MemberType.IsUnmanaged
                 || property.Invoker.MemberType == TypeRecord.String
                 || property.Invoker.MemberType.OriginType == TypeRecord.String)
                {
                    source.Append($$"""

                        {{property.LeftArg}} = value.{{property.Invoker.MemberName}};        
        """);
                }
            }
            source.Append($$"""
               
                    }
                }
        """);
        }

        source.Append($$"""

                if (count == {{serializeProperties.Count}})
                {
        """);

        foreach (var property in serializeProperties)
        {
            if (property.Invoker.MemberType.IsUnmanaged)
            {
                unmanagedBatch.Add(property);
                if (unmanagedBatch.Count == maxUnmanagedBatchSize)
                    AppendReadUnmanagedBatch();

            }
            else if (property.Invoker.MemberType.IsArray
                && property.Invoker.MemberType.OriginType?.IsUnmanaged is true)
            {
                source.Append($$"""

                    reader.ReadUnmanagedArray(ref {{property.RightArg}});
        """);
            }
            else if (property.Invoker.MemberType == TypeRecord.String
              || property.Invoker.MemberType.OriginType == TypeRecord.String)
            {
                AppendReadUnmanagedBatch();
                source.Append($$"""

                    {{property.LeftArg}} = reader.ReadString()!;
        """);
            }
            else
            {
                AppendReadUnmanagedBatch();
                source.Append($$"""

                    {{property.FieldName}}.Serializer.Read(ref reader, ref {{property.RightArg}});
        """);
            }
        }
        AppendReadUnmanagedBatch();

        source.Append($$"""

                }
                else if (count == 0) goto result;
                else
                {
        """);
        int i = 1;
        foreach (var property in serializeProperties)
        {
            if (property.Invoker.MemberType.IsUnmanaged)
            {
                var method = property.Invoker.MemberType.IsNullable ? "DangerousReadUnmanaged"
                    : "ReadUnmanaged";
                source.Append($$"""

                    reader.{{method}}({{(method == "ReadUnmanagedArray" ? "ref" : "out")}} {{property.LeftArg}}); if (count == {{i++}}) goto result;
        """);
            }
            else if (property.Invoker.MemberType.IsArray
                && property.Invoker.MemberType.OriginType?.IsUnmanaged is true)
            {
                source.Append($$"""

                    reader.ReadUnmanagedArray(ref {{property.RightArg}}); if (count == {{i++}}) goto result;
        """);
            }
            else if (property.Invoker.MemberType == TypeRecord.String
              || property.Invoker.MemberType.OriginType == TypeRecord.String)
            {
                source.Append($$"""

                    {{property.LeftArg}} = reader.ReadString()!; if (count == {{i++}}) goto result;
        """);
            }
            else
            {
                source.Append($$"""

                    {{property.FieldName}}.Serializer.Read(ref reader, ref {{property.RightArg}}); if (count == {{i++}}) goto result;
        """);
            }
        }
        source.Append($$"""

                }
                result:
        """);
        AppendConstructor(type, serializeProperties, constructor, hasInitOnly);
        source.Append($$"""
        
            }
        """);

        void AppendWriteUnmanagedBatch()
        {
            if (unmanagedBatch.Count == 0)
                return;

            if (unmanagedBatch.Count == 1)
            {
                var memberType = unmanagedBatch[0].Invoker.MemberType;
                var method = memberType.IsNullable ? "DangerousWriteUnmanaged" : "WriteUnmanaged";
                source.Append($$"""

                writer.{{method}}(value.{{unmanagedBatch[0].Invoker.MemberName}});
        """);
            }
            else
            {
                source.Append($$"""

                writer.DangerousWriteUnmanaged({{string.Join(",\n                    ", unmanagedBatch
                    .Select(x => $"value.{x.Invoker.MemberName}"))}});
        """);
            }

            unmanagedBatch.Clear();
        }

        void AppendReadUnmanagedBatch()
        {
            if (unmanagedBatch.Count == 0)
                return;

            if (unmanagedBatch.Count == 1)
            {
                var memberType = unmanagedBatch[0].Invoker.MemberType;
                var method = memberType.IsNullable ? "DangerousReadUnmanaged" : "ReadUnmanaged";
                source.Append($$"""

                    reader.{{method}}(out {{unmanagedBatch[0].LeftArg}});
        """);
            }
            else
            {
                source.Append($$"""

                    reader.DangerousReadUnmanaged({{string.Join(",\n                    ", unmanagedBatch
                    .Select(x => $"out {x.LeftArg}"))}});
        """);
            }

            unmanagedBatch.Clear();
        }

    }

    private void GenerateMessagePack(TypeRecord type, List<SerializeInvokerInfo> serializeProperties, CtorInfo constructor, bool hasInitOnly)
    {
        source.Append($$"""

            public override void Write(ref MessagePackWriter writer, scoped ref {{type}}{{(type.IsValueType ? string.Empty : "?")}} value, MessagePackSerializerOptions options)
            {
        """);
        if (!type.IsValueType)
        {
            source.Append($$"""

                if (value == null)
                {
                    writer.WriteNil();
                    return;
                }
        """);
        }
        source.Append($$"""

                writer.WriteArrayHeader({{serializeProperties.Count}});
        """);
        foreach (var property in serializeProperties)
        {
            if (property.Invoker.MemberType == TypeRecord.String
              || property.Invoker.MemberType.OriginType == TypeRecord.String
              || property.Invoker.MemberType == TypeRecord.Int16
              || property.Invoker.MemberType == TypeRecord.Int32
              || property.Invoker.MemberType == TypeRecord.Int64
              || property.Invoker.MemberType == TypeRecord.Byte
              || property.Invoker.MemberType == TypeRecord.SByte
              || property.Invoker.MemberType == TypeRecord.Boolean
              || property.Invoker.MemberType == TypeRecord.Char
              || property.Invoker.MemberType == TypeRecord.UInt16
              || property.Invoker.MemberType == TypeRecord.UInt32
              || property.Invoker.MemberType == TypeRecord.UInt64
              || property.Invoker.MemberType == TypeRecord.Single
              || property.Invoker.MemberType == TypeRecord.Double)
            {
                source.Append($$"""

                writer.Write(value.{{property.Invoker.MemberName}});
        """);
            }
            else
            {
                source.Append($$"""

                {{property.FieldName}}.Write(ref writer, ref value, options);
        """);
            }
        }

        source.Append($$"""
        
            }

            public override void Read(ref MessagePackReader reader, scoped ref {{type}}{{(type.IsValueType ? string.Empty : "?")}} value, MessagePackSerializerOptions options)
            {
        """);
        source.Append($$"""

                if (reader.TryReadNil())
                {
                    {{(type.IsValueType ? string.Empty : "value = null!;")}}
                    return;
                }
                var count = reader.ReadArrayHeader();
        """);


        foreach (var property in serializeProperties)
        {
            if (type.IsValueType)
            {
                source.Append($$"""

                {{property.Invoker.MemberType}} {{property.LeftArg}} = value.{{property.Invoker.MemberName}};
        """);
            }
            else
            {
                source.Append($$"""

                {{property.Invoker.MemberType}} {{property.LeftArg}} = default{{(
                    property.Invoker.MemberType.IsValueType ? string.Empty : "!")}};
        """);
            }
        }

        if (!type.IsValueType)
        {
            source.Append($$"""
        
                if (value != null)
                {
        """);
            foreach (var property in serializeProperties)
            {
                if (!property.Invoker.MemberType.IsUnmanaged
                 && property.Invoker.MemberType != TypeRecord.String
                 && property.Invoker.MemberType.OriginType != TypeRecord.String)
                {
                    source.Append($$"""

                    {{property.LeftArg}} = value.{{property.Invoker.MemberName}};        
        """);
                }
            }
            source.Append($$"""

                    if (count < {{serializeProperties.Count}})
                    {
        """);
            foreach (var property in serializeProperties)
            {
                if (property.Invoker.MemberType.IsUnmanaged
                 || property.Invoker.MemberType == TypeRecord.String
                 || property.Invoker.MemberType.OriginType == TypeRecord.String)
                {
                    source.Append($$"""

                        {{property.LeftArg}} = value.{{property.Invoker.MemberName}};        
        """);
                }
            }
            source.Append($$"""
               
                    }
                }
        """);
        }

        source.Append($$"""

                if (count == {{serializeProperties.Count}})
                {
        """);

        foreach (var property in serializeProperties)
        {
            if (property.Invoker.MemberType == TypeRecord.String
              || property.Invoker.MemberType.OriginType == TypeRecord.String)
            {
                source.Append($$"""

                    {{property.LeftArg}} = reader.ReadString()!;
        """);
            }
            else
            {
                source.Append($$"""

                    {{property.FieldName}}.Serializer.Read(ref reader, ref {{property.RightArg}}, options);
        """);
            }
        }

        source.Append($$"""

                }
                else if (count == 0) goto result;
                else
                {
        """);
        int i = 1;
        foreach (var property in serializeProperties)
        {
            if (property.Invoker.MemberType == TypeRecord.String
              || property.Invoker.MemberType.OriginType == TypeRecord.String)
            {
                source.Append($$"""

                    {{property.LeftArg}} = reader.ReadString()!; if (count == {{i++}}) goto result;
        """);
            }
            else
            {
                source.Append($$"""

                    {{property.FieldName}}.Serializer.Read(ref reader, ref {{property.RightArg}}, options); if (count == {{i++}}) goto result;
        """);
            }
        }
        source.Append($$"""

                }
                result:
        """);
        AppendConstructor(type, serializeProperties, constructor, hasInitOnly);
        source.Append($$"""
        
            }
        """);

    }

    private void AppendConstructor(TypeRecord type, List<SerializeInvokerInfo> serializeProperties, CtorInfo constructor, bool hasInitOnly)
    {
        if (!type.IsValueType && !hasInitOnly)
        {
            source.Append($$"""

                if (value == null)
                {
        """);
        }
        if (!type.IsValueType || hasInitOnly)
        {
            AppendConstructor(serializeProperties, constructor);
        }
        if (!type.IsValueType && !hasInitOnly)
        {
            source.Append($$"""
        
                }
                else
                {
        """);
        }
        if (!hasInitOnly)
        {
            foreach (var property in serializeProperties)
            {
                source.Append($$"""

                    value.{{property.Invoker.MemberName}} = {{property.RightArg}};
        """);

            }

        }
        if (!type.IsValueType && !hasInitOnly)
        {
            source.Append($$"""
        
                }
        """);
        }
    }

    private void AppendConstructor(List<SerializeInvokerInfo> serializeProperties, CtorInfo constructor)
    {
        source.Append($$"""

                value = new({{string.Join(",\n            ", constructor.Constructor.Parameters
                                .Select(x =>
                                {
                                    var property = serializeProperties.FirstOrDefault(p => p.CtorParam == x.Name);
                                    return property == null ? "default!" : $"{x.Name}: {property.RightArg}";
                                }))}})
        """);
        var nonCtorProperties = serializeProperties.Where(x => string.IsNullOrEmpty(x.CtorParam));
        if (nonCtorProperties.Any())
        {
            source.Append($$"""
        
                {
        """);
            foreach (var property in nonCtorProperties)
            {
                source.Append($$"""

                    {{property.Invoker.MemberName}} = {{property.RightArg}},
        """);
            }

            source.Append($$"""
        
                };
        """);
        }
        else
        {
            source.Append(@";
                    ");
        }
    }


    private void Register()
    {
        source.Append($$"""

            public {{(existInvokers.Count > 0 || Type.IsInterface ? "new" : string.Empty)}} static IElementSerializer RegisterSerializer()
            {
                RegisterInvokers();
               
        """);
        foreach (var typeInfo in typeInvokers)
        {
            var type = typeInfo.Type;
            source.Append($$"""

                var _{{type.OriginName}}Serializer = {{GetSerializerName(type)}}.Instance;
                SerializeProvider.Register(_{{type.OriginName}}Serializer);

        """);
        }
        var first = typeInvokers.First().Type;
        source.Append($$"""
                    
                return _{{first.OriginName}}Serializer;
            }
        """);
    }

    private string GetDefault(InvokerInfo invoker)
    {
        if (invoker.Member is PropertyRecord property)
        {
            if (property.GetAttribute("DefaultValueAttribute") is { } defaultAttribute)
            {

            }
        }
        return "default";
    }

    public static int? GetOrder(InvokerInfo invoker)
    {
        if (invoker.Member == null)
            return null;
        if (invoker.Member.GetAttribute(Helper.cColumnAttribute) is { } column
            && column.GetNamedValue("Order").Value is int columnOrder
            && columnOrder != -1)
            return columnOrder;
        if (invoker.Member.GetAttribute(Helper.cElementSerializerAttribute) is { } serializer
            && serializer.GetNamedValue("Order").Value is int serializerOrder)
            return serializerOrder;
        if (invoker.Member.GetAttribute("DataMemberAttribute") is { } dataMember
            && dataMember.GetNamedValue("Order").Value is int dataMemeberOrder
            && dataMemeberOrder != 0)
            return dataMemeberOrder;
        if (invoker.Member.GetAttribute("MemoryPackOrderAttribute") is { } memPack
            && memPack.ConstructorArguments.FirstOrDefault().Value is int memPackOrder)
            return memPackOrder;
        if (invoker.Member.GetAttribute("KeyAttribute") is { } messagePack
            && messagePack.ConstructorArguments.FirstOrDefault().Value is int messagePackOrder)
            return messagePackOrder;
        return null;
    }

    private string GetKeys(InvokerInfo invoker, bool? nonSerialize = null, bool? isReadOnly = null)
    {
        var result = "SerializePropertyKeys.None";
        if (IsSerializeAttribute(invoker.MemberType))
            result += "\n                        | SerializePropertyKeys.Attribute";
        if (IsXMLAttributed(invoker.Member!))
            result += "\n                        | SerializePropertyKeys.XmlAttributed";
        if (IsRequired(invoker.Member!))
            result += "\n                        | SerializePropertyKeys.Required";
        if (IsJsonSynchronized(invoker.Member!))
            result += "\n                        | SerializePropertyKeys.ChangeSensitive";
        if (isReadOnly ?? IsReadOnly(invoker.Member!))
            result += "\n                        | SerializePropertyKeys.ReadOnly";
        if (IsInitOnly(invoker.Member!))
            result += "\n                        | SerializePropertyKeys.InitOnly";
        if (nonSerialize ?? IsNonSerialize(invoker))
            result += "\n                        | SerializePropertyKeys.NonSerialize";
        return result;
    }

    public static bool IsSerializeAttribute(InvokerInfo invoker)
        => IsSerializeAttribute(invoker.MemberType);

    public static bool IsSerializeAttribute(TypeRecord forType)
    {
        var type = forType.IsNullable ? forType.OriginType! : forType;
        return type.IsSystemType || type.IsEnum
           || (type.IsArray && (type.OriginType == TypeRecord.Byte || type.OriginType == TypeRecord.Char))
           || type.OriginName == nameof(CultureInfo) || type.OriginName == nameof(Version);
    }

    public static bool IsXMLAttributed(InvokerInfo invoker)
        => IsXMLAttributed(invoker.Member!);

    public static bool IsXMLAttributed(AttributedRecord property)
    {
        return property.GetAttribute("XmlAttributeAttribute>") != null
            || property.GetAttribute("XmlTextAttribute") != null
            || property.GetAttribute("XmlElementAttribute") != null
            || property.GetAttribute("XmlEnumAttribute") != null
            || property.Type is { } type && IsXMLAttributed(type);
    }

    public static bool IsRequired(AttributedRecord info)
    {
        if (info is FieldRecord fieldRecord && fieldRecord.IsRequired)
            return true;
        return info.GetAttribute("RequiredAttribute") != null;
    }

    public static bool IsJsonSynchronized(AttributedRecord property)
    {
        return property.GetAttribute("JsonSynchronizedAttribute") != null;
    }

    public static bool IsReadOnly(AttributedRecord member)
    {
        if (member is PropertyRecord property)
        {
            if (property.IsReadOnly)
                return true;
        }
        return member.GetAttribute("ReadOnlyAttribute")?.GetNamedValue("IsReadOnly").Value is true;
    }

    public static bool IsInitOnly(AttributedRecord member)
    {
        if (member is PropertyRecord property)
        {
            return property.IsInitOnly;
        }
        if (member is FieldRecord field)
        {
            return field.IsInitOnly;
        }
        return false;
    }

    public static bool IsNonSerialize(InvokerInfo invoker)
    {
        if (invoker.Member!.GetAttribute(Helper.cElementSerializerAttribute) is { } elementSerializer
            && elementSerializer.GetNamedValue("Ignore").Value is bool ignore
            && ignore)
            return true;

        var itemType = invoker.MemberType;

        if (itemType.IsSubclassOf(nameof(Delegate))
            || (itemType == invoker.DeclareType && itemType.IsValueType)
            || (invoker.Member is PropertyRecord propertyInfo && propertyInfo.IsIndex)
            || string.Equals(invoker.MemberName, "BindingContext", StringComparison.Ordinal))
            return true;
        return (invoker.Member!.GetAttribute("XmlIgnoreAttribute")
                ?? invoker.Member!.GetAttribute("JsonIgnoreAttribute")
                ?? invoker.Member!.GetAttribute("NonSerializedAttribute")) != null;
    }

    private CtorInfo GetConstructor(TypeRecord type)
    {
        return ctors.Values.FirstOrDefault(x => x.DeclareType == type);
    }

    private string FormatConstructor(CtorInfo ctor)
    {
        return ctor != null
            ? $"{ctor.DeclareType}.{(ctor.HasInstance ? $"{ctor.Name}.Instance" : $"new {ctor.Name}()")}"
            : "null!";
    }

    private static ITypeSymbol? TryGetSerializerType(AttributedRecord attributedRecord)
    {
        var attributeData = attributedRecord.GetAttribute(Helper.cElementSerializerAttribute);
        var argIgnore = attributeData?.GetNamedValue("SerializerType") ?? default;
        return !argIgnore.IsNull ? (ITypeSymbol?)argIgnore.Value : null;
    }

    private string GetSerializer()
    {
        var serializerType = TryGetSerializerType(Type);
        return serializerType != null
            ? $"{(IsInstance ? $"{serializerType.ToDisplayString()}.Instance" : $"new {serializerType.ToDisplayString()}()")}"
            : $"new TypeInfoSerializer<{Type}>(typeInfo)";
    }

    private string GetSerializer(InvokerInfo value)
        => value.Member != null
            ? GetSerializer(value.Member)
            : "null!";

    private string GetSerializer(AttributedRecord proeprtyOrField)
    {
        var serializerType = TryGetSerializerType(proeprtyOrField);
        return serializerType != null
            ? $"{(IsInstance ? $"{serializerType.ToDisplayString()}.Instance" : $"new {serializerType.ToDisplayString()}()")}"
            : GetSerializer(proeprtyOrField.TypeRef?.Type ?? TypeRecord.Object);
    }

    private string GetSerializer(TypeRecord type)
    {
        var serializerType = TryGetSerializerType(type);
        return serializerType != null
                ? $"{(IsInstance ? $"{serializerType.ToDisplayString()}.Instance" : $"new {serializerType.ToDisplayString()}()")}"
            : type == Type || type.OriginType == Type || type.BaseType == Type
                ? "null"
            : IsISerializeRegister(type) ? type.IsNullable && type.IsValueType
                ? type.IsUnmanaged ? $"SerializeProvider.RegisterUnmanagedNullable<{type.OriginType!.Name}>()"
                    : $"SerializeProvider.RegisterNullable<{type.OriginType!.Name}>()"
                : $"SerializeProvider.Register<{type.Name}>()"
            : type.IsArray ? type.OriginType?.IsUnmanaged is true
                ? $"SerializeProvider.RegisterUnmanagedArray<{(type.IsNullable ? type.OriginType!.OriginType!.Name : type.OriginType!.Name)}>()!"
                : $"SerializeProvider.RegisterArray<{(type.IsNullable ? type.OriginType!.OriginType!.Name : type.OriginType!.Name)}>()"
            : type.IsSystemType || type.IsNullable && type.OriginType?.IsSystemType is true
                ? $"SerializeProvider.RegisterSystem<{type}>()"
            : type.IsEnum ? type.IsNullable
                ? $"SerializeProvider.RegisterNullableEnum<{type.OriginType!.Name}>()"
                : $"SerializeProvider.RegisterEnum<{type.Name}>()"
            : type.IsImplementInterface("IDictionary")
                ? $"SerializeProvider.RegisterDictionary<{type}, {string.Join(", ", type.GenericParameters.Select(p => p.Type?.Name))}>()"
            : type.IsImplementInterface("IEnumerable")
                ? $"SerializeProvider.RegisterCollection<{type}, {string.Join(", ", type.GenericParameters.Select(p => p.Type?.Name))}>()"
            : $"SerializeProvider.GetSerializer<{type}>()";
    }

    private static bool IsISerializeRegister(TypeRecord type)
    {
        return type.IsImplementInterface(Helper.cISerializerRegister)
            || type.GetInvokerAttribute(allowInvoker: false) != null;
    }
}
