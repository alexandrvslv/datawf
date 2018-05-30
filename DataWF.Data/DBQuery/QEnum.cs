/*
 QItem.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
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
