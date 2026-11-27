using System;

namespace DataWF.Invokers.Defaults;

public class ToStringInvoker<T> : Invoker<T, string?>, IInvokerRegister
{
    public static readonly ToStringInvoker<T> Instance = new();

    public static void RegisterInvokers()
    {
        InvokeProvider.RegisterInvoker<T>(Instance);
    }

    public override string Name => nameof(Object.ToString);

    public override bool CanWrite => false;

    public override string? GetValue(scoped in T target) => target?.ToString();

    public override void SetValue(scoped ref T target, string? value) { }
}

public class ToStringInvoker : Invoker<object, string?>
{
    public static readonly ToStringInvoker Instance = new();

    public override string Name => nameof(Object.ToString);

    public override bool CanWrite => false;

    public override string? GetValue(scoped in object target) => target?.ToString();

    public override void SetValue(scoped ref object target, string? value) { }
}
