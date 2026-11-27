namespace DataWF.Invokers;

public delegate V GetIndexHandler<T, K, V>(scoped in T target, K index);
public delegate void SetIndexHandler<T, K, V>(scoped ref T target, K index, V value);
public class ActionIndexInvoker<T, V, K>(string name, GetIndexHandler<T, K, V> getAction, SetIndexHandler<T, K, V>? setAction = null)
    : IndexInvoker<T, V, K>
{
    public GetIndexHandler<T, K, V> GetAction => getAction;
    public SetIndexHandler<T, K, V>? SetAction => setAction;
    public override bool CanWrite => SetAction != null;

    public override string Name => name;

    public override V GetValue(scoped in T target, K index) => GetAction(in target, index);

    public override void SetValue(scoped ref T target, K index, V value) => SetAction?.Invoke(ref target, index, value);
}
