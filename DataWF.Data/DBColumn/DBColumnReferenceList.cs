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
using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public sealed class DBColumnReferenceList : SelectableList<DBColumnReference>
    {
        public DBColumnReferenceList()
        {
            //Indexes.Add("Column");
        }

        public DBColumnReferenceList(IEnumerable<DBColumn> columns) : this()
        {
            AddRangeInternal(columns.Select(p => new DBColumnReference { Column = p }), false);
        }

        [XmlIgnore, JsonIgnore]
        public DBTableItem Container { get; set; }

        [XmlIgnore, JsonIgnore]
        public IDBSchema Schema { get { return Container?.Schema; } }

        [XmlIgnore, JsonIgnore]
        public string Names { get; private set; }

        public void Add(DBColumn column)
        {
            Add(new DBColumnReference { Column = column });
        }

        public void Add(string column)
        {
            Add(new DBColumnReference { ColumnName = column });
        }

        public bool Remove(DBColumn column)
        {
            var refernce = Get(column);
            if (refernce != null)
            {
                Remove(refernce);
                return true;
            }
            return false;
        }

        public bool Contains(DBColumn column)
        {
            return Contains(column.FullName);
        }

        public bool Contains(string column)
        {
            return Get(column) != null;
        }

        public DBColumnReference Get(DBColumn column)
        {
            return Get(column.FullName);
        }

        public DBColumnReference Get(string column)
        {
            return Select(DBColumnReference.ColumnNameInvoker.Instance, CompareType.Equal, column).FirstOrDefault();
        }

        public void Replace(DBColumn exist, DBColumn replacer)
        {
            foreach (var item in this)
            {
                if (item.Column == exist)
                    item.Column = replacer;
            }
        }

        protected override void OnPropertyChanged(string property)
        {
            Names = string.Empty;
            foreach (var element in items)
            {
                Names += element.ColumnName.Substring(element.ColumnName.LastIndexOf('.') + 1);
                if (!IsLast(element))
                    Names += ", ";
            }
            base.OnPropertyChanged(property);
        }
    }


}