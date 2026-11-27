using System;

namespace DataWF.Invokers.Defaults;

public class GroupInvoker<T> : Invoker<T, T> where T : IGroup
{
    public static readonly GroupInvoker<T> Instance = new();

    public GroupInvoker()
    { }

    public override bool CanWrite => true;

    public override string Name => nameof(IGroup.Group);

    public override T GetValue(scoped in T target) => (T)target.Group;

    public override void SetValue(scoped ref T target, T value) => target.Group = value;
}

