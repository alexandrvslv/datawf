//  The MIT License (MIT)
//
// Copyright © 2020 Vassilyev Alexandr
//
//   email:alexandr_vslv@mail.ru
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the “Software”), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
//
// THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
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
