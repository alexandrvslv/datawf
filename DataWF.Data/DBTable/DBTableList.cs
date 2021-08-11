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
using System.Collections.Generic;
using DataWF.Common;

namespace DataWF.Data
{
    public class DBTableList : DBSchemaItemList<DBTable>
    {
        private IListIndex<DBTable, string> itemTypeNameIndex;

        public DBTableList() : this(null)
        { }

        public DBTableList(DBSchema schema) : base(schema)
        {
            Indexes.Add(DBTable.GroupNameInvoker.Instance);
            itemTypeNameIndex = Indexes.Add(DBTable.ItemTypeNameInvoker.Instance);
            ApplyDefaultSort();
        }

        public IEnumerable<DBTable> GetByGroup(string name)
        {
            return Select(nameof(DBTable.GroupName), CompareType.Equal, name);
        }

        public DBTable GetByTypeName(string name)
        {
            return itemTypeNameIndex.SelectOne(name);
        }

        public override int AddInternal(DBTable item)
        {
            item.Schema = Schema;
            var index = base.AddInternal(item);
            if (item.IsVirtual)
            {
                item.ParentTable.AddVirtual(item);
            }
            return index;
        }

        public void ApplyDefaultSort()
        {
            ApplySort(DBTableComparer.Instance);
        }
    }
}
