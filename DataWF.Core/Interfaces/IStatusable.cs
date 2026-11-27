using DataWF.Invokers;

namespace DataWF;

[Invoker(Instance = true)]
public interface IStatusable
{
    DBStatus Status { get; set; }
}


