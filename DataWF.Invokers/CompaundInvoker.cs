using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace DataWF.Invokers;

public class CompaundInvoker<T, V> : Invoker<T, V>
{
    private readonly string property;
    private readonly List<IInvoker> invokers = new();
    private readonly IInvoker lastInvoker;
    public CompaundInvoker(string property)
       : this(property, typeof(T).GetMemberInfos(property))
    { }

    public CompaundInvoker(string property, List<MemberInvokeInfo> list)
    {
        this.property = property;
        foreach (var info in list)
        {
            invokers.Add(InvokeProvider.GetInvoker(info.Info, info.Index));
        }
        lastInvoker = invokers.Last();
    }

    public override string Name => property;

    public override bool CanWrite => lastInvoker.CanWrite;

    public override V GetValue(scoped in T target)
    {
        var temp = (object?)null;
        var span = CollectionsMarshal.AsSpan(invokers);
        for (var i = 0; i < span.Length; i++)
        {
            var invoker = span[i];
            temp = invoker.GetValue(temp ?? target!);
            if (temp == null)
                return default!;
        }
        return (V)temp!;
    }

    public override void SetValue(scoped ref T target, V value)
    {
        var temp = (object?)null;
        var span = CollectionsMarshal.AsSpan(invokers);
        for (var i = 0; i < span.Length - 1; i++)
        {
            var invoker = span[i];
            temp = invoker.GetValue(temp ?? target!);
            if (temp == null)
                return;
        }
        if (temp != null)
            lastInvoker.SetValue(temp, value);
    }
}
