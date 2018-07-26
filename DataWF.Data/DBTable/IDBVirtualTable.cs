/*
 DBTable.cs
 
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
using System.Collections.Specialized;
using System.ComponentModel;

namespace DataWF.Data
{
    public interface IDBVirtualTable
    {
        DBTable BaseTable { get; set; }
        QQuery FilterQuery { get; }
        void OnBaseChanged(DBItem item, string property, NotifyCollectionChangedAction type);
        void OnItemChanging(DBItem item, string property, DBColumn column, object value);
        void OnItemChanged(DBItem item, string property, DBColumn column, object value);
    }
}
