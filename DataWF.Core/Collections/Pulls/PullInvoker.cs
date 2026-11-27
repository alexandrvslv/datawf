using DataWF.Invokers;

namespace DataWF.Collections.Pulls;

public class PullInvoker<T, V> : Invoker<T, V> where T : IPullHandler
{
    public GenericPull<V> pull;
    public PullInvoker(GenericPull<V> pull)
    {
        this.pull = pull;
    }

    public override string Name => pull.Name;

    public override bool CanWrite => true;

    public override V GetValue(scoped in T target)
        => pull.GetValue(target.GetRefHandler());

    public override void SetValue(scoped ref T target, V value)
        => pull.SetValue(target.GetRefHandler(), value);
}
