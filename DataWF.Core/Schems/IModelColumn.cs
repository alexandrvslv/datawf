using DataWF.Collections.Indexes;
using DataWF.Collections.Query;
using DataWF.Invokers;
using DataWF.Serialization;
using System.Reflection;

namespace DataWF.Models;

public interface IModelColumn: ISerializePropertyInfo, IQueryInvoker
{
    IModelTable Table { get; }
    IIndex? Index { get; }
    PropertyInfo? PropertyInfo { get; }
    bool IsReference { get; }

}
