using DataWF.Collections.Indexes;
using DataWF.Collections.Query;
using DataWF.Invokers;
using DataWF.Serialization;
using System;
using System.Reflection;

namespace DataWF.Models;

public class ModelColumn<T, V> : SerializePropertyInfo<T, V>, IModelColumn<T, V>
{
    public ModelColumn(
        IInvoker<T, V> invoker,
        SerializePropertyKeys keys,
        string? name = null,
        V? defaultValue = default,
        IElementSerializer<V>? serializer = null,
        int order = -1
        )
        : base(invoker, keys, name, defaultValue, serializer, order)
    {
    }
    
    public ModelColumn(string name, int order = -1) : base(name, order)
    {
    }

    
    [ElementSerializer(Ignore = true)]
    public IModelTable<T> Table { get; set; }

    [ElementSerializer(Ignore = true)]
    public IIndex<T, V>? Index { get; set; }

    public PropertyInfo? PropertyInfo { get; set; }

    

}
