using DataWF.Invokers;
using System;
using System.Collections.Generic;

namespace DataWF.Serialization;

public interface ISerializeTypeInfo
{
    Type Type { get; }
    string TypeName { get; }
    string ShortName { get; }
    ushort Union { get; }
    IRuntimeConstructor Constructor { get; }
    SerializeTypeKeys Keys { get; }
    bool IsAttribute { get; }
    IReadOnlyCollection<ISerializePropertyInfo> Properties { get; }
    IElementSerializer GetSerializer();

    ISerializePropertyInfo? GetProperty(string name);
    ISerializePropertyInfo? GetProperty(int order);

    void AddDescending(IElementSerializer hashSetInfo);

    IElementSerializer? GetDescending(string name);

    IElementSerializer? GetDescending(Type type);

    IElementSerializer? GetDescending(ushort union);

    void ValidateSerializer();
}

public interface IOutSerializeTypeInfo<out T> : ISerializeTypeInfo
{
    new IRuntimeConstructor<T> Constructor { get; }

    // new IOutSerializeTypeInfo<T>? GetDescending(string name);
    //
    // new IOutSerializeTypeInfo<T>? GetDescending(Type type);
    //
    // new IOutSerializeTypeInfo<T>? GetDescending(ushort union);

}

public interface ISerializeTypeInfo<T> : IOutSerializeTypeInfo<T>
{
    new ISerializePropertyInfo<T>[] Properties { get; }
    ISerializePropertyInfo<T>[] SerializeProperties { get; }
    ISerializePropertyInfo<T>[] XmlProperties { get; }
    new IElementSerializer<T> GetSerializer();
    Dictionary<string, ISerializePropertyInfo<T>>.AlternateLookup<ReadOnlySpan<byte>> GetLookup();

    new ISerializePropertyInfo<T>? GetProperty(string name);
    new ISerializePropertyInfo<T>? GetProperty(int order);

    IElementSerializer ISerializeTypeInfo.GetSerializer() => GetSerializer();
    IRuntimeConstructor ISerializeTypeInfo.Constructor => Constructor;

    IReadOnlyCollection<ISerializePropertyInfo> ISerializeTypeInfo.Properties => Properties;

    ISerializePropertyInfo? ISerializeTypeInfo.GetProperty(string name)
        => GetProperty(name);
    ISerializePropertyInfo? ISerializeTypeInfo.GetProperty(int order)
        => GetProperty(order);


}

