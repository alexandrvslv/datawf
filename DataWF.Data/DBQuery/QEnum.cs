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

namespace DataWF.Data
{
    public class QEnum : QItem, IQItemList
    {
        protected QItemList<QItem> items;

        public QEnum()
        {
            Items = new QItemList<QItem>();
        }

        public QEnum(IList list, DBColumn column = null) : this()
        {
            foreach (var item in list)
            {
                if (item is QItem)
                    items.Add((QItem)item);
                else
                    items.Add(QParam.Fabric(item, column));
            }
        }

        public QItemList<QItem> Items
        {
            get { return items; }
            set
            {
                items = value;
                items.Indexes.Clear();
                items.Owner = this;
            }
        }

        public IQItemList Owner => null;

        public override object GetValue(DBItem row = null)
        {
            return Items;
        }

        public override string Format(IDbCommand command = null)
        {
            string rez = string.Empty;
            foreach (QItem item in Items)
            {
                rez += item.Format(command);
                if (!items.IsLast(item))
                    rez += ", ";
            }
            if (rez.Length > 0)
                rez = "(" + rez + ")";
            //DBService.FormatToSqlText(value)
            return rez;
        }

        public void Delete(QItem item)
        {
            Items.Delete(item);
        }
    }
}
