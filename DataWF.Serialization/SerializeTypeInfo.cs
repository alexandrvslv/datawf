using DataWF.Invokers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataWF.Serialization;

public class SerializeTypeInfo<T> : ISerializeTypeInfo<T>
{
    private readonly Dictionary<string, ISerializePropertyInfo<T>> nameIndex;
    private readonly Dictionary<int, ISerializePropertyInfo<T>> orderIndex;
    private readonly ISerializePropertyInfo<T>[] properties;
    private readonly ISerializePropertyInfo<T>[] serialProperties = null!;
    private ISerializePropertyInfo<T>[] writeXmlProperties = null!;
    private HashSet<IElementSerializer>? descendings = null;
    private ushort union;
    private Dictionary<string, ISerializePropertyInfo<T>>.AlternateLookup<ReadOnlySpan<byte>> nameLookup;

    public SerializeTypeInfo(
        IRuntimeConstructor<T> constructor,
        IReadOnlyCollection<ISerializePropertyInfo<T>> properties,
        SerializeTypeKeys keys = SerializeTypeKeys.None,
        string? typeName = null,
        string? shortName = null,
        IReadOnlyCollection<IElementSerializer>? descendings = null)
    {
        TypeName = typeName ?? typeof(T).Name;
        ShortName = shortName ?? typeof(T).FormatShort();
        union = ShortName.GetFnv1aUShortHash();
        Keys = keys;
        Constructor = constructor;
        this.properties = [.. properties.Select((x, i) => x.WithOrder(x.Order > -1 ? x.Order : i))
            .OrderBy(x => x.Order)];
        nameIndex = new(properties.Count, Utf8StringOrdinalIgnoreCaseComparer.Instance);
        orderIndex = new(properties.Count);
        foreach (var property in properties)
        {
            nameIndex[property.Name] = property;
            orderIndex[property.Order] = property;
        }

        nameLookup = nameIndex.GetAlternateLookup<ReadOnlySpan<byte>>();
        serialProperties = this.properties.Where(x => !x.IsNonSerialize).ToArray();
        if (descendings != null)
        {
            foreach (var descendant in descendings)
                AddDescending(descendant);
        }
    }

    public Type Type => typeof(T);

    public string TypeName { get; }

    public string ShortName { get; internal set; }

    public ushort Union => union;

    public Dictionary<string, ISerializePropertyInfo<T>>.AlternateLookup<ReadOnlySpan<byte>> GetLookup()
        => nameLookup;

    public IRuntimeConstructor<T> Constructor { get; }

    public SerializeTypeKeys Keys { get; }

    public bool IsAttribute => (Keys & SerializeTypeKeys.Attribute) != 0;

    public ISerializePropertyInfo<T>[] Properties
    {
        get => properties;
        private set { }
    }

    public ISerializePropertyInfo<T>[] SerializeProperties
    {
        get => serialProperties;
    }

    public ISerializePropertyInfo<T>[] XmlProperties
    {
        get => writeXmlProperties ??=
        [
            .. Properties.Where(x => !x.IsNonSerialize)
                .OrderBy(x => x, XmlPropertiesComparer.Instance)
        ];
        set => writeXmlProperties = value;
    }

    public IElementSerializer<T> GetSerializer() => SerializeProvider.GetSerializer<T>()!;

    public ISerializePropertyInfo<T>? GetProperty(string name)
        => nameIndex.TryGetValue(name, out var value) ? value : null;

    
    public ISerializePropertyInfo<T>? GetProperty(int order)
        => orderIndex.TryGetValue(order, out var value) ? value : null;


    public void AddDescending(IElementSerializer serializer)
    {
        (descendings ??= []).Add(serializer);
    }

    public virtual IElementSerializer? GetDescending(string name)
        => descendings?.FirstOrDefault(x => x.TypeInfo.ShortName == name);

    public virtual IElementSerializer? GetDescending(Type type)
        => descendings?.FirstOrDefault(x => x.TypeInfo.Type == type)
           ?? SerializeProvider.GetSerializer(type);

    public virtual IElementSerializer? GetDescending(ushort union)
        => descendings?.FirstOrDefault(x => x.TypeInfo.Union == union);

    public void ValidateSerializer()
    {
        foreach (var property in serialProperties)
            property.ValidateSerializer();
    }
}

public class XmlPropertiesComparer : IComparer<ISerializePropertyInfo>
{
    public static readonly XmlPropertiesComparer Instance = new();

    public int Compare(ISerializePropertyInfo? x, ISerializePropertyInfo? y)
    {
        var result = y?.IsAttribute.CompareTo(x?.IsAttribute ?? false) ?? 0;
        result = result == 0 ? (x?.Order ?? 0).CompareTo(y?.Order ?? 0) : result;
        return result;
    }
}

public class OrderPropertiesComparer : IComparer<ISerializePropertyInfo>
{
    public static readonly OrderPropertiesComparer Instance = new();
    public int Compare(ISerializePropertyInfo? x, ISerializePropertyInfo? y) => x?.Order.CompareTo(y?.Order ?? 0) ?? 0;
}

[Flags]
public enum SerializeTypeKeys : byte
{
    None = 0,
    Attribute = 1,
}

public static class HashExtensions
{
    public static ushort GetFnv1aUShortHash(this ReadOnlySpan<char> input)
    {
        uint hash = 2166136261; // FNV-1a offset basis

        foreach (char c in input)
        {
            // Process the low byte of the character
            hash ^= (byte)(c & 0xFF);
            hash *= 16777619;

            // Process the high byte of the character
            hash ^= (byte)(c >> 8);
            hash *= 16777619;
        }

        unchecked
        {
            // Safely downcast the 32-bit hash to a 16-bit unsigned int
            return (ushort)hash;
        }
    }
}