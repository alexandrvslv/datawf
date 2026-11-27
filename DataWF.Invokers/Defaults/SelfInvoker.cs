namespace DataWF.Invokers.Defaults;

public class SelfInvoker<T> : Invoker<T, T>, IInvokerRegister
{
    public static readonly SelfInvoker<T> Instance = new();

    public static void RegisterInvokers()
    {
        InvokeProvider.RegisterInvoker<T>(Instance);
    }

    public override bool CanWrite => false;

    public override string Name => "this";

    public override T GetValue(scoped in T target) => target;

    public override void SetValue(scoped ref T target, T value) { }

}

public class SelfInvoker : Invoker<object, object>
{
    public static readonly SelfInvoker Instance = new();

    public override bool CanWrite => false;

    public override string Name => "this";

    public override object GetValue(scoped in object target) => target;

    public override void SetValue(scoped ref object target, object value) { }

}
