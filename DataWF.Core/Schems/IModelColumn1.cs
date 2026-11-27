using DataWF.Collections.Indexes;
using DataWF.Invokers;
using DataWF.Serialization;

namespace DataWF.Models;

public interface IModelColumn<T, V> : IModelColumn, ISerializePropertyInfo<T,V>, IQueryInvoker<T, V>
{
    new IIndex<T, V>? Index { get; }

    IIndex? IModelColumn.Index => Index;

    new IModelTable<T> Table { get; }

    IModelTable IModelColumn.Table => Table;

}
