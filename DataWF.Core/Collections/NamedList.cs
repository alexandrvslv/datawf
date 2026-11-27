
using DataWF.Collections.Indexes;
using DataWF.Comparers;
using DataWF.Invokers;
using System.Collections.Generic;

namespace DataWF.Collections;

public class NamedList<T> : SelectableList<T>, INamedList<T>
{
    protected InvokerIndex<T, string> nameIndex;
    protected IInvokerGetter<T, string> nameInvoker;

    public NamedList(int capacity, InvokerIndex<T, string> nameIndex)
        : base(capacity)
    {
        this.nameIndex = nameIndex;
        this.nameInvoker = nameIndex.Invoker;
        Indexes.Add(nameIndex.Invoker.Name, nameIndex);
    }

    public NamedList(int capacity, IInvokerGetter<T, string> nameInvoker) : this(capacity, new InvokerIndex<T, string>(
            nameInvoker,
            IndexFactory.GetNullKey<string>(),
            DefaultComparer<T>.Default,
            UniversalComparer<string>.Default,
            false))
    { }

    public NamedList(IInvokerGetter<T, string> nameInvoker) : this(4, nameInvoker)
    { }

    public NamedList(string? Name = null)
        : this(InvokeProvider.GetInvoker<T, string>(Name ?? "Name"))
    { }

    public NamedList(IEnumerable<T> items) : this()
    {
        AddRangeInternal(items, false);
    }

    public virtual T? this[string param]
    {
        get => nameIndex.SelectOne(param);
        set
        {
            var exists = this[param];
            if (!value?.Equals(exists) ?? false)
            {
                if (exists != null)
                {
                    Remove(exists);
                }
                Add(value);
            }
        }
    }

    public virtual bool Contains(string name)
    {
        return this[name] != null;
    }

    public bool Remove(string name)
    {
        var item = this[name];
        return item == null ? false : Remove(item);
    }

    public T? Get(string name)
    {
        return this[name];
    }

    public void Set(T value)
    {
        if (value != null)
        {
            this[nameInvoker.GetValue(value)] = value;
        }
    }

    public void Set(T value, int index)
    {
        if (value != null)
        {
            Set(value);
            if (index >= Count)
            {
                index = Count - 1;
            }
            this[index] = (T)value;
        }
    }

}
