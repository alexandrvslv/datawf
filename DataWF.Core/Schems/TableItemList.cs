using DataWF.Collections;
using DataWF.Models;

namespace DataWF;

public class TableItemList<T> : ChangeableList<T>, IModelTableItemList
{
    public TableItemList(IModelTable<T> table)
    {
        Table = table;
    }

    public IModelTable<T> Table { get; }

    IModelTable IModelTableItemList.Table => Table;
}