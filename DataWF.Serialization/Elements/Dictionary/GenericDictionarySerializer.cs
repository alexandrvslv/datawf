using System;

namespace DataWF.Serialization.Elements;

public abstract class GenericDictionarySerializer<T, K, V> : ElementSerializer<T>
    where K : notnull
{
    protected IElementSerializer<K> keySerializer;
    protected IElementSerializer<V> valueSerializer;

    protected GenericDictionarySerializer(ISerializeTypeInfo<T> typeInfo) : base(typeInfo)
    {
        keySerializer = SerializeProvider.GetSerializer<K>();
        valueSerializer = SerializeProvider.GetSerializer<V>();
    }

    public override bool IsAttributeSerializer => false;

    public IElementSerializer<K> KeySerializer => keySerializer;

    public IElementSerializer<V> ValueSerializer => valueSerializer;


    public override T FromString(ReadOnlySpan<char> value)
    {
        throw new NotImplementedException();
    }

    public override string ToString(T value)
    {
        throw new NotImplementedException();
    }

    public override string FormatDisplay(T value, string? format = null)
    {
        throw new NotImplementedException();
    }

    public override T Parse(object value)
    {
        throw new NotImplementedException();
    }
    
}
