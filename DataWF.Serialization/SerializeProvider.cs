using DataWF.Invokers;
using DataWF.Invokers.Defaults;
using DataWF.Serialization.Elements;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;

namespace DataWF.Serialization;

public class SerializeProvider
{
    internal static ConcurrentDictionary<Type, IElementSerializer> Serializers { get; } = new(-1, 100);
    internal static ConcurrentDictionary<string, IElementSerializer> NameIndex { get; } = new(-1, 100);
    internal static ConcurrentDictionary<ushort, IElementSerializer> UnionIndex { get; } = new(-1, 100);
    private static readonly ConcurrentDictionary<MetadataToken, IElementSerializer> cachePropertySerializer =
        new(-1, 200);

    static SerializeProvider()
    {
        Register(ObjectSerializer.Instance);
        //RegisterSystem(TempFileStreamSerializer.Instance);
    }


    public static IElementSerializer<V>? GetSerializer<T, V>(string property, Type? serializerType = null)
    {
        //return ValueSerializer.GetSerializerFor(property);
        var token = MetadataToken.GetToken<T>(property);
        if (!cachePropertySerializer.TryGetValue(token, out var serializer))
        {
            if (serializerType != null)
            {
                serializer = CreateSerializer(typeof(V), serializerType);
            }
            else
            {
                serializer = GetSerializer<V>()!;
            }

            cachePropertySerializer[token] = serializer;
        }

        return (IElementSerializer<V>?)serializer;
    }

    private static IElementSerializer CreateSerializer(Type type, Type serializerType)
    {
        if (serializerType.IsGenericTypeDefinition)
        {
            serializerType = serializerType.MakeGenericType(type);
        }

        return (IElementSerializer)(InvokeProvider.CreateObject(serializerType)
            ?? throw new InvalidOperationException(
                $"Cannot create instance of {serializerType.FullName}"));
    }

    public static IElementSerializer<T> Register<T>(IElementSerializer<T> serializer)
    {
        if (Cache<T>.Serializer == serializer)
            return serializer;
        var typeInfo = serializer.TypeInfo;
        Cache<T>.Serializer = serializer;
        Cache<T>.TypeInfo = typeInfo;
        typeInfo.ValidateSerializer();
        Serializers[typeInfo.Type] = serializer;
        NameIndex[typeInfo.ShortName] = serializer;
        UnionIndex[typeInfo.Union] = serializer;
        return serializer;
    }

    public static IElementSerializer Register(IElementSerializer serializer)
    {
        var parameterTypes = new Type[] { typeof(IElementSerializer<>).MakeGenericType(serializer.TypeInfo.Type) };
        return (IElementSerializer)typeof(SerializeProvider)
            .GetMethod(nameof(Register), BindingFlags.Public | BindingFlags.Static, parameterTypes)!
            .MakeGenericMethod(serializer.TypeInfo.Type)
            .Invoke(null, [serializer])!;
    }

    public static IElementSerializer<T> Register<T>()
        where T : ISerializerRegister?
    {
        return Cache<T>.Serializer ?? (IElementSerializer<T>)T.RegisterSerializer();
    }

    public static IElementSerializer<T?> RegisterNullable<T>()
        where T : struct, ISerializerRegister
    {
        return Cache<T?>.Serializer ?? RegisterDefaultNullable((IElementSerializer<T>)T.RegisterSerializer());
    }

    public static IElementSerializer<T?> RegisterUnmanagedNullable<T>()
        where T : unmanaged, ISerializerRegister
    {
        return Cache<T?>.Serializer ?? RegisterDefaultUnmanagedNullable((IElementSerializer<T>)T.RegisterSerializer());
    }

    public static IElementSerializer<T?> RegisterDefaultNullable<T>()
        where T : struct
        => RegisterDefaultNullable<T>(null);

    public static IElementSerializer<T?> RegisterDefaultNullable<T>(IElementSerializer<T>? valueSerializer)
        where T : struct
    {
        if (Cache<T?>.Serializer is NullableSerializer<T> serializer && serializer.ValueSerializer == valueSerializer)
            return serializer;
        return Register(valueSerializer == null ? NullableSerializer<T>.Instance : new NullableSerializer<T>(valueSerializer));
    }

    public static IElementSerializer<T?> RegisterDefaultUnmanagedNullable<T>()
        where T : unmanaged
        => RegisterDefaultUnmanagedNullable<T>(null);

    public static IElementSerializer<T?> RegisterDefaultUnmanagedNullable<T>(IElementSerializer<T>? valueSerializer)
        where T : unmanaged
    {
        if (Cache<T?>.Serializer is UnmanagedNullableSerializer<T> serializer && serializer.ValueSerializer == valueSerializer)
            return serializer;
        return Register(valueSerializer == null ? UnmanagedNullableSerializer<T>.Instance : new UnmanagedNullableSerializer<T>(valueSerializer));
    }

    public static IElementSerializer<T[]?> RegisterArray<T>()
        => Cache<T[]?>.Serializer ?? ArraySerializers.Register<T>();

    public static IElementSerializer<T[]?> RegisterUnmanagedArray<T>() where T : unmanaged
        => Cache<T[]?>.Serializer ?? ArraySerializers.RegisterUnmanaged<T>();

    public static IElementSerializer<T> RegisterSystem<T>()
        => Cache<T>.Serializer ?? SystemSerializers.Register<T>();

    public static IElementSerializer<T?> RegisterCollection<T, TElement>()
        where T : IEnumerable<TElement>?
        => Cache<T?>.Serializer ?? Elements.CollectionSerializers<TElement>.Register<T>();

    public static IElementSerializer<T> RegisterDictionary<T, TKey, TValue>() //where T : IDictionary<K, V>
        where TKey : notnull
        => Cache<T>.Serializer ?? Elements.DictionarySerializers<TKey, TValue>.Register<T>();

    public static IElementSerializer<T> RegisterEnum<T>() where T : unmanaged, Enum
        => Cache<T>.Serializer ?? EnumSerializers<T>.Register(typeof(T).GetEnumUnderlyingType());

    public static IElementSerializer<T?> RegisterNullableEnum<T>() where T : unmanaged, Enum
        => Cache<T?>.Serializer ?? EnumSerializers<T>.RegisterDefaultNullable(typeof(T).GetEnumUnderlyingType());

    public static IElementSerializer RegisterBinarySerializable<T>() where T : IBinarySerializable
        => Cache<T>.Serializer ?? Register(new BinarySerializer<T>());

    public static IElementSerializer<T> GetSerializer<T>()
        => Cache<T>.Serializer ?? (IElementSerializer<T>)GetSerializer(typeof(T));

    public static IElementSerializer<T>? GetRegistered<T>()
        => Cache<T>.Serializer;

    public static IElementSerializer GetSerializer(Type type)
    {
        if (Serializers.TryGetValue(type, out var serializer)) return serializer;
        var elementType = type.CheckNullable();
        if (elementType.IsAssignableTo(typeof(ISerializerRegister)))
        {
            if (elementType != type)
            {
                if (elementType.IsUnmanagedCustom())
                {
                    return (IElementSerializer)typeof(SerializeProvider)
                        .GetMethod(nameof(RegisterUnmanagedNullable), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                        .MakeGenericMethod(elementType)
                        .Invoke(null, null)!;
                }
                return (IElementSerializer)typeof(SerializeProvider)
                    .GetMethod(nameof(RegisterNullable), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                    .MakeGenericMethod(elementType)
                    .Invoke(null, null)!;
            }
            return (IElementSerializer)typeof(SerializeProvider)
                .GetMethod(nameof(Register), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                .MakeGenericMethod(elementType)
                .Invoke(null, null)!;
        }

        var attribute = elementType.GetCustomAttribute<ElementSerializerAttribute>(false);
        if (attribute != null && attribute.SerializerType != null)
            serializer = CreateSerializer(elementType, attribute.SerializerType);
        else if (elementType.IsPrimitive
                 || SystemSerializers.ContainsKey(elementType))
        {
            if (type != elementType && SystemSerializers.ContainsKey(type))
            {
                return (IElementSerializer)typeof(SerializeProvider)
                    .GetMethod(nameof(RegisterSystem), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                    .MakeGenericMethod(type)
                    .Invoke(null, null)!;
            }
            return (IElementSerializer)typeof(SerializeProvider)
                .GetMethod(nameof(RegisterSystem), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                .MakeGenericMethod(elementType)
                .Invoke(null, null)!;
        }
        else if (elementType.IsEnum)
        {
            if (elementType != type)
            {
                return (IElementSerializer)typeof(SerializeProvider)
                    .GetMethod(nameof(RegisterNullableEnum), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                    .MakeGenericMethod(elementType)
                    .Invoke(null, null)!;
            }
            return (IElementSerializer)typeof(SerializeProvider)
                .GetMethod(nameof(RegisterEnum), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                .MakeGenericMethod(elementType)
                .Invoke(null, null)!;
        }
        else if (typeof(ICustomSerializable).IsAssignableFrom(elementType))
        {
            serializer =
                (IElementSerializer)InvokeProvider.CreateObject(
                    typeof(CustomSerializer<>).MakeGenericType(elementType))!;
        }
        else if (typeof(IBinarySerializable).IsAssignableFrom(elementType))
        {
            serializer = (IElementSerializer)typeof(SerializeProvider)
                .GetMethod(nameof(RegisterBinarySerializable), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                .MakeGenericMethod(elementType)
                .Invoke(null, null)!;
        }
        else if (elementType.IsArray && elementType.GetElementType() is { } arrayElementType)
        {
            if (arrayElementType.IsUnmanagedCustom())
            {
                return (IElementSerializer)typeof(SerializeProvider)
                    .GetMethod(nameof(RegisterUnmanagedArray), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                    .MakeGenericMethod(arrayElementType)
                    .Invoke(null, null)!;
            }
            return (IElementSerializer)typeof(SerializeProvider)
                .GetMethod(nameof(RegisterArray), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                .MakeGenericMethod(arrayElementType)
                .Invoke(null, null)!;
        }
        else if (elementType.IsAnyDictionary() && elementType.IsGenericType)
        {
            return (IElementSerializer)typeof(SerializeProvider).GetMethod(nameof(RegisterDictionary),
                    BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                .MakeGenericMethod(elementType, elementType.GenericTypeArguments[0],
                    elementType.GenericTypeArguments[1])
                .Invoke(null, null)!;
        }
        else if (typeof(IEnumerable).IsAssignableFrom(elementType) && elementType.IsGenericType)
        {
            return (IElementSerializer)typeof(SerializeProvider).GetMethod(nameof(RegisterCollection),
                    BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                .MakeGenericMethod(elementType, elementType.GenericTypeArguments[0])
                .Invoke(null, null)!;
        }
        else
        {
            if (elementType.IsUnmanagedCustom())
            {
                if (elementType != type)
                {
                    serializer = (IElementSerializer)InvokeProvider.CreateObject(
                        typeof(UnmanagedNullableTypeInfoSerializer<>).MakeGenericType(elementType))!;
                }
                else
                {
                    serializer = (IElementSerializer)InvokeProvider.CreateObject(
                        typeof(UnmanagedTypeInfoSerializer<>).MakeGenericType(elementType))!;
                }
            }
            else
            {
                serializer = (IElementSerializer)InvokeProvider.CreateObject(
                    typeof(TypeInfoSerializer<>).MakeGenericType(elementType))!;
            }
            Register(serializer);
            return serializer;
        }

        return CheckNullable(serializer, type, elementType);

        static IElementSerializer CheckNullable(IElementSerializer serializer, Type type, Type elementType)
        {
            if (elementType != type)
            {
                if (elementType.IsUnmanagedCustom())
                    serializer = (IElementSerializer)typeof(SerializeProvider)
                        .GetMethod(nameof(RegisterDefaultUnmanagedNullable), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                        .MakeGenericMethod(elementType)
                        .Invoke(null, null)!;
                else
                    serializer = (IElementSerializer)typeof(SerializeProvider)
                        .GetMethod(nameof(RegisterDefaultNullable), BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!
                        .MakeGenericMethod(elementType)
                        .Invoke(null, null)!;
            }

            return serializer;
        }
    }

    public static ISerializeTypeInfo<T> GetTypeInfo<T>()
    {
        if (Cache<T>.TypeInfo != null)
            return Cache<T>.TypeInfo;
        var serializer = GetSerializer<T>();
        return serializer.TypeInfo;
    }

    public static ISerializeTypeInfo GetTypeInfo(Type type)
    {
        if (!Serializers.TryGetValue(type, out var serializer))
        {
            serializer = GetSerializer(type);
        }
        return serializer.TypeInfo;
    }

    public static T? Clone<T>(scoped ref T obj, bool ignoreEnumerable)
    {
        if (obj == null)
        {
            return default;
        }

        var typeInfo = GetTypeInfo<T>();
        var newItem = typeInfo.Constructor.Create();
        foreach (var property in typeInfo.Properties)
        {
            if (ignoreEnumerable && TypeHelper.IsList(property.Invoker.DataType))
                continue;
            property.Copy(ref obj, ref newItem);
        }

        return newItem;
    }

    internal static class Cache<T>
    {
        public static IElementSerializer<T>? Serializer;
        public static ISerializeTypeInfo<T>? TypeInfo;
    }
}