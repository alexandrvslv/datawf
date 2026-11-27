using DataWF.Invokers;
using MemoryPack;
using MessagePack;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataWF.Serialization;

public class SerializePropertyInfo<T, V> : ISerializePropertyInfo<T, V>
{
    private bool? isAttribute;
    private JsonIgnoreCondition? ignoreCondition;

    public SerializePropertyInfo(
        IInvoker<T, V> invoker,
        SerializePropertyKeys keys,
        string? name = null,
        V? defaultValue = default,
        IElementSerializer<V>? serializer = null,
        int order = -1)
    {
        Invoker = invoker;
        Keys = keys;
        Name = name ?? invoker.Name;
        DefaultValue = defaultValue;
        Serializer = serializer!;
        Order = order;        
    }

    public SerializePropertyInfo(string name, int order = -1)
        : this(
            InvokeProvider.GetInvoker<T, V>(name),
            SerializePropertyKeys.None,
            order: order)
    { }

    public SerializePropertyInfo(PropertyInfo property, int order = -1)
        : this(
            invoker: InvokeProvider.GetInvoker<T, V>(property),
            keys: TypeHelper.GetSerializePropertyKeys(property),
            defaultValue: TypeHelper.GetDefault(property) is V defaultValue ? defaultValue : default,
            order: TypeHelper.GetOrder(property, order)
        )
    { }

    [ElementSerializer(SerializerType = typeof(InvokerSerializer<,>))]
    public IInvoker<T, V> Invoker { get; set; }

    public string Name
    {
        get => field;
        set
        {
            field = value;
            JsonName = JsonEncodedText.Encode(value, JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
        }
    }

    [ElementSerializer(Ignore = true)]
    public JsonEncodedText JsonName { get; set; }

    [ElementSerializer(Ignore = true)]
    public Type DataType => typeof(V);

    [ElementSerializer(Ignore = true)]
    public bool IsReference => DataType.IsAssignableFrom(typeof(INotifyPropertyChanged));

    [ElementSerializer(Ignore = true)]
    public Type TargetType => typeof(T);

    [ElementSerializer(Ignore = true)]
    public bool CanWrite => Invoker.CanWrite;

    public SerializePropertyKeys Keys { get; init; }

    [ElementSerializer(Ignore = true)]
    public bool IsAttribute => isAttribute ??=
        (Keys & SerializePropertyKeys.Attribute) == SerializePropertyKeys.Attribute
        || Serializer.IsAttributeSerializer;

    [ElementSerializer(Ignore = true)]
    public bool IsXmlAttributed => (Keys & SerializePropertyKeys.XmlAttributed) == SerializePropertyKeys.XmlAttributed;

    [ElementSerializer(Ignore = true)]
    public bool IsChangeSensitive =>
        (Keys & SerializePropertyKeys.ChangeSensitive) == SerializePropertyKeys.ChangeSensitive;

    [ElementSerializer(Ignore = true)]
    public bool IsRequired => (Keys & SerializePropertyKeys.Required) == SerializePropertyKeys.Required;

    [ElementSerializer(Ignore = true)]
    public bool IsText => (Keys & SerializePropertyKeys.Text) == SerializePropertyKeys.Text;

    [ElementSerializer(Ignore = true)]
    public bool IsNonSerialize => (Keys & SerializePropertyKeys.NonSerialize) == SerializePropertyKeys.NonSerialize;

    [ElementSerializer(Ignore = true)]
    public bool IsReadOnly => (Keys & SerializePropertyKeys.ReadOnly) == SerializePropertyKeys.ReadOnly;

    public JsonIgnoreCondition? IgnoreCondition
    {
        get => ignoreCondition;
        set => ignoreCondition = value;
    }

    public int Order { get; set; }

    [ElementSerializer(Ignore = true)]
    public bool HasDefault => DefaultValue != null;

    public V? DefaultValue { get; set; }

    [ElementSerializer(Ignore = true)]
    public IElementSerializer<V> Serializer { get; set; }

    public override string ToString() => $"{Order} {Name} {DataType.Name} {Keys}";

    public bool CheckDefault(ref T value) => CheckDefault(Invoker.GetValue(in value));

    public bool CheckDefault(V value) => DefaultValue?.Equals(value) ?? false;

    public void Copy(scoped ref T value, scoped ref T newItem)
    {
        Invoker.Copy(in value, ref newItem);
    }

    public ISerializePropertyInfo<T> WithOrder(int i)
    {
        Order = i;
        return this;
    }

    public void ValidateSerializer()
    {
        Serializer ??= SerializeProvider.GetSerializer<V>();
    }

    public V GetValue(scoped in T target) => Invoker.GetValue(in target);

    public void SetValue(scoped ref T target, V value) => Invoker.SetValue(ref target, value);

}
