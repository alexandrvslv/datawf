namespace DataWF.Invokers;

public delegate V GetHandler<T, V>(scoped in T target);
public delegate void SetHandler<T, V>(scoped ref T target, V value);

public class ActionInvoker<T, V>(string name, GetHandler<T, V> getAction, SetHandler<T, V>? setAction = null)
    : Invoker<T, V>
{
    public override string Name { get => name; }

    public override bool CanWrite => SetAction != null;

    public GetHandler<T, V> GetAction
    {
        get => getAction;
        protected set => getAction = value;
    }

    public SetHandler<T, V>? SetAction
    {
        get => setAction;
        protected set => setAction = value;
    }

    public override V GetValue(scoped in T target) => GetAction(in target);

    public override void SetValue(scoped ref T target, V value) => SetAction?.Invoke(ref target, value);
}

