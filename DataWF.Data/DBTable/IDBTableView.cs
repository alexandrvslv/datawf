/*
 DBView.cs
 
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
using DataWF.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public interface IDBTableView : IDBTableContent, ISortable, INotifyListPropertyChanged, IDisposable, IFilterable
    {
        IDbCommand Command { get; set; }

        QParam DefaultParam { get; set; }

        QQuery Query { get; set; }

        DBStatus StatusFilter { get; set; }

        Type TypeFilter { get; set; }

        bool ClearFilter();

        void ResetFilter();

        bool IsEdited { get; }

        bool IsStatic { get; set; }

        new bool IsSynchronized { get; set; }

        void OnTableChanged(DBItem item, NotifyCollectionChangedAction type);

        void OnItemChanged(DBItem item, string property, DBColumn column);

        IEnumerable<DBItem> Load(DBLoadParam param = DBLoadParam.None);

        void LoadAsynch(DBLoadParam param = DBLoadParam.None);

        Task Save();

        IList ToList();
    }
}
