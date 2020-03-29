using System;


namespace DataWF.Data
{

    public static class DBPullIndexFabric
    {
        public static PullIndex Create(DBTable list, DBColumn column)
        {
            if (column.DataType == null)
                throw new ArgumentException($"Type is null on column {column.FullName}");

            //Type gtype = typeof(DBNullablePullIndex<>).MakeGenericType(column.DataType);
            return PullIndexFabric.Create(column.Pull, list.ItemType.Type, column.DataType, list.DefaultComparer);
        }
    }

    //public class DBPullIndex<K> : PullIndex<DBItem, K>
    //{
    //    public DBPullIndex(Pull pull, object nullKey, IComparer valueComparer = null, IEqualityComparer keyComparer = null) : base(pull, nullKey, valueComparer, keyComparer)
    //    {
    //    }

    //    public override void Refresh(ListChangedType type, DBItem row)
    //    {
    //        if (type == ListChangedType.Reset)
    //            Refresh();
    //        else if (type == ListChangedType.ItemAdded)
    //            Add(row);
    //        else if (type == ListChangedType.ItemDeleted && row != null)
    //            Remove(row);
    //    }
    //}

    //public interface IDBItemIndex
    //{
    //}

    //public class DBItemPullIndex<K> : PullIndex<DBItem, K>
    //{
    //    private DBTable table;

    //    public DBItemPullIndex(DBTable table, DBColumn column)
    //        : this(table, column.Pull)
    //    { }

    //    public DBItemPullIndex(DBTable table, Pull pull, object nullKey):base(pull, )
    //    {
    //        this.table = table;            
    //    }
    //}
}
