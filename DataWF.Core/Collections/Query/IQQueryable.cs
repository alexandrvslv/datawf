using System.Linq;

namespace DataWF.Collections.Query;

public interface IQueryProvider: System.Linq.IQueryProvider
{
    IQQuery Query();
}

public interface IQueryProvider<out T> : IQueryProvider
{
    new IQQuery<T> Query();
}
