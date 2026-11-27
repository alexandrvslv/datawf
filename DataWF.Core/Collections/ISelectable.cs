using DataWF.Collections.Indexes;
using DataWF.Collections.Query;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Collections;

public interface ISelectable : ISortable, Query.IQueryProvider, INotifyListPropertyChanged
{
    IIndexList Indexes { get; }
    IFilterable DefaultView { get; }
    object NewItem();
    
    void AddRange(IEnumerable items);
    void RemoveRange(IEnumerable items);

}

public interface ISelectable<T> : ISortable<T>, IQueryProvider<T>, ISelectable
{
    new IIndexList<T> Indexes { get; }
    IIndexList ISelectable.Indexes => Indexes;
    new IFilterable<T> DefaultView { get; }
    IFilterable ISelectable.DefaultView => DefaultView;

    new T NewItem();
    object ISelectable.NewItem() => NewItem()!;
    
    void AddRange(IEnumerable<T> items);
    void RemoveRange(IEnumerable<T> items);
}
