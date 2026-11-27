using DataWF.Collections;
using System.Collections;

namespace DataWF.Models;

public interface IModelItemList : ICollection, INamedList
{
    IModelSchema Schema { get; }
}

public interface IModelItemList<out T> : IModelItemList, INamedListGetter<T> where T : IModelItem
{
    T this[string name] { get; }
}