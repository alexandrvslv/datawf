/*
 DBRow.cs
 
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

using System;
using System.ComponentModel;

namespace DataWF.Data
{
    public class DBVirtualItem : DBItem
    {
        [NonSerialized]
        protected DBItem main;

        public DBVirtualItem()
        { }

        public IDBVirtualTable VirtualTable
        {
            get { return (IDBVirtualTable)Table; }
        }

        [Browsable(false)]
        public override DBTable Table
        {
            get { return base.Table; }
            set { table = value; }
        }

        [Browsable(false)]
        public DBItem Main
        {
            get { return main; }
            set
            {
                main = value;
                if (main != null)
                {
                    main.PropertyChanged += OnMainPropertyChanged;
                    hindex = main.hindex;
                }
            }
        }

        public override DBUpdateState DBState
        {
            get { return main.DBState; }
            set { main.DBState = value; }
        }

        public override void Build(DBTable table, DBUpdateState state = DBUpdateState.Insert, bool def = true)
        {
            if (Main == null)
            {
                Main = ((IDBVirtualTable)table).BaseTable.NewItem(state, def);
            }
            else if (Main.Table != ((IDBVirtualTable)table).BaseTable)
            {
                throw new Exception("Build VirtualItem Fail! Main.Table != BaseTable");
            }
            base.Build(table, state, def);
        }

        public override void RemoveIndex(DBColumn column, object value)
        {
            //base.RemoveIndex(column, value);
            if (column.Table != main.Table)
                column = ((DBVirtualColumn)column).BaseColumn;
            main.RemoveIndex(column, value);
        }

        public override void AddIndex(DBColumn column, object value)
        {
            if (column.Table != main.Table)
                column = ((DBVirtualColumn)column).BaseColumn;
            main.AddIndex(column, value);
        }

        public override bool GetIsChanged()
        {
            return main.GetIsChanged();
        }

        internal protected override void RemoveOld()
        {
            main.RemoveOld();
        }

        internal protected override void RemoveTag()
        {
            main.RemoveTag();
        }

        public override void Reject()
        {
            main.Reject();
        }

        public override void OnPropertyChanged(string property, DBColumn column = null, object value = null)
        {
            main.OnPropertyChanged(property, column, value);
        }

        public override void Dispose()
        {
            base.Dispose();
            main.PropertyChanged -= OnMainPropertyChanged;
        }

        private void OnMainPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Attached)
                table.OnItemChanged(this, e.PropertyName, ListChangedType.ItemChanged);
            // raise events
            //TODO PropertyChanged?.Invoke(this, e);
        }
    }
}

