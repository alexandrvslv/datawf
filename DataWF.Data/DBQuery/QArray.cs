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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using System;

namespace DataWF.Data
{
    public class QArray : QItem, IQItemList
    {
        protected QItemList<QItem> items;
        protected DBColumn column;

        public QArray()
        {
            Items = new QItemList<QItem>(this);
        }

        public QArray(IEnumerable list, DBColumn column = null) : this()
        {
            Column = column;
            foreach (var item in list)
            {
                if (item is QColumn
                    || item is DBColumn
                    || item is QItem qItem && qItem.IsReference)
                    IsReference = true;

                var value = item;
                if (item is string stringed)
                    value = stringed.Trim('\'');

                items.Add(QItem.Fabric(value, column));
            }
        }

        public DBColumn Column
        {
            get => column;
            set
            {
                if (column != value)
                {
                    column = value;
                    foreach (var item in items)
                    {
                        if (item is QValue qValue
                            && qValue.Column == null)
                            qValue.Column = column;
                    }
                }
            }
        }

        public QItemList<QItem> Items
        {
            get => items;
            set
            {
                items = value;
                items.Indexes.Clear();
                items.Owner = this;
            }
        }

        [XmlIgnore, JsonIgnore]
        public IQItem Owner => Container?.Owner;

        public override IQItemList Container
        {
            get => base.Container;
            set
            {
                base.Container = value;
                if (Query != null)
                {
                    foreach (var item in items)
                    {
                        if (item is QColumn column
                            && column.QTable == null
                            && column.Table != null)
                        {
                            column.QTable = Query.GetTable(column.Table);
                        }
                    }
                }
            }
        }

        public override object GetValue(DBItem row = null)
        {
            return Items;
        }

        public override string Format(IDbCommand command = null)
        {
            //if (command != null && Column != null)
            //{
            //    return $"({base.CreateParameter(command, Items.Select(p => p.GetValue()).ToList(), Column)})";
            //}

            var rez = new StringBuilder();
            foreach (QItem item in Items)
            {
                rez.Append(item.Format(command));
                rez.Append(", ");
            }
            if (rez.Length > 0)
            {
                rez.Length -= 2;
                return $"({rez})";
            }
            //DBService.FormatToSqlText(value)
            return string.Empty;
        }

        public void Add(QItem item)
        {
            Items.Add(item);
        }

        public void Delete(QItem item)
        {
            Items.Delete(item);
        }

        public IEnumerable<IT> GetAllQItems<IT>(Func<IT, bool> predicate = null) where IT : IQItem
        {
            return Items.GetAllQItems<IT>(predicate);
        }
    }
}
