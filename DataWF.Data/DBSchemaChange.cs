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
using DataWF;
using System;
using System.Collections.Generic;

namespace DataWF.Data
{
    [Invoker(Instance = true)]
    public partial class DBSchemaChange : ICheck, IComparable<DBSchemaChange>
    {
        private DBSchemaItem item;
        private DDLType change;
        private bool check = true;

        public string Schema { get; set; }

        public string Table { get; set; }

        public string Type
        {
            get { return item == null ? null : Locale.Get(item.GetType()); }
        }

        public DBSchemaItem Item
        {
            get { return item; }
            set
            {
                item = value;
                if (item.Schema != null)
                {
                    Schema = item.Schema?.Name;
                    if (item is IDBTableContent tableContent)
                    {
                        Table = tableContent.Table?.Name;
                    }
                }
            }
        }

        public DBSchemaItemType ItemType => Item?.Type ?? DBSchemaItemType.None;

        public DDLType Change
        {
            get { return change; }
            set { change = value; }
        }

        public string Generate(bool dependency)
        {
            return item.FormatSql(change, dependency);
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", change, Type, item);
        }

        public int CompareTo(DBSchemaChange other)
        {
            var result = Comparer<DDLType>.Default.Compare(this.Change, other.Change);
            if (result == 0)
            {
                result = Comparer<DBSchemaItemType>.Default.Compare(this.ItemType, other.ItemType);
                if (result == 0)
                {
                    if (this.Item is DBTable thisTable && other.Item is DBTable otherTable)
                        result = DBTableComparer.Instance.Compare(thisTable, otherTable);
                    else if (this.Item is DBColumn thisColumn && other.Item is DBColumn otherColumn)
                        result = thisColumn.Order.CompareTo(otherColumn.Order);
                    else
                        result = this.Item.Name.CompareTo(other.Item.Name);
                }
                else if (this.ItemType == DBSchemaItemType.View
                   && this.Change == DDLType.Drop)
                    result = 1;
                else if (other.ItemType == DBSchemaItemType.View
                    && other.Change == DDLType.Drop)
                    result = -1;
            }
            else if (this.ItemType == DBSchemaItemType.View
                || other.ItemType == DBSchemaItemType.View)
                result = -result;
            return result;
        }

        public bool Check
        {
            get { return check; }
            set { check = value; }
        }

        public int Order { get; set; }
    }
}
