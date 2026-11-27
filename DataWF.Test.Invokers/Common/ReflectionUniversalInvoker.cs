using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataWF.Invokers;

public class ReflectionUniversalInvoker<T, V> : IInvoker<T, V>
{
    public ReflectionUniversalInvoker(string name)
    {
        Name = name;
        var type = typeof(T);
        List = type.GetMemberInfos(name);
        Last = List.Last();
        DataType = Last.Info.GetMemberType();
        TargetType = type;
        CanWrite = Last.Info is FieldInfo || (Last.Info is PropertyInfo pInfo && pInfo.CanWrite);
    }

    public string Name { get; set; }

    public bool CanWrite { get; set; }

    public Type DataType { get; set; }

    public Type TargetType { get; set; }

    public List<MemberInvokeInfo> List { get; private set; }

    public MemberInvokeInfo Last { get; private set; }

    public object? GetValue(object? target)
    {
        var temp = target;
        foreach (var info in List)
        {
            temp = GetValue(info, temp);
            if (temp == null)
                break;
        }
        return temp;
    }

    public void SetValue(object? target, object? value)
    {
        object? temp = null;
        foreach (var info in List)
        {
            if (info == Last)
            {
                SetValue(info, target, value);
                if (info.Info.DeclaringType!.IsValueType && temp != null)
                {
                    SetValue(List[List.Count - 2], temp, target);
                }
            }
            else
            {
                temp = target;
                target = GetValue(info, target);
                if (target == null)
                    break;
            }
        }
    }

    public void SetValue(MemberInvokeInfo info, object? target, object? value)
    {
        switch (info.Info)
        {
            case PropertyInfo pInfo:
                if (info.Index != null)
                {
                    pInfo.SetValue(target, value, new object[] { info.Index });
                }
                else
                {
                    pInfo.SetValue(target, value);
                }
                break;
            case FieldInfo fInfo:
                fInfo.SetValue(target, value);
                break;
        }
    }

    public object? GetValue(MemberInvokeInfo info, object? target) => info.Info switch
    {
        PropertyInfo pInfo => info.Index != null ? pInfo.GetValue(target, new object[] { info.Index }) : pInfo.GetValue(target),
        FieldInfo fInfo => fInfo.GetValue(target),
        MethodInfo mInfo => mInfo.Invoke(target, null),
        _ => null,
    };

    public V GetValue(T target) => (V)GetValue((object?)target)!;

    public V GetValue(scoped in T target) => (V)GetValue((object?)target)!;

    V IInvokerGetter<V>.GetValue(object? target) => (V)GetValue(target)!;

    public void SetValue(T target, V value) => SetValue((object?)target, (object?)value);

    public void SetValue(scoped ref T target, V value) => SetValue((object?)target, (object?)value);

    public void SetValue(object? target, V value) => SetValue(target, (object?)value);
}
