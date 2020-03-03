/*
 DBETable.cs
 
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
using System.ComponentModel;

namespace DataWF.Data
{
    public class DBETable : IComparable, INotifyPropertyChanged, ICheck
    {
        private DBExport _export;
        private bool complete = false;
        private string query = "";
        private string source = "";
        private string target = "";
        private bool check = true;
        //private bool drop = false;
        private readonly DBEColumnList columns;

        public DBETable()
        {
            columns = new DBEColumnList(this);
        }

        public DBETable(string name) : this()
        {
            source =
                target = name;
        }

        [Browsable(false)]
        public DBExport Export
        {
            get { return _export; }
            set { _export = value; }
        }

        public override string ToString()
        {
            return source + " - " + target;
        }

        [DisplayName("Source Code")]
        public string Source
        {
            get { return source; }
            set
            {
                if (source == value)
                    return;
                source = value;
                OnPropertyChanged(nameof(Source));
            }
        }

        [DisplayName("Source Table")]
        public DBTable SourceTable
        {
            get { return _export?.Source?.Tables[source]; }
            set
            {
                source = value?.Name;
                OnPropertyChanged(nameof(SourceTable));
            }
        }

        [DisplayName("Destination Code")]
        public string Target
        {
            get { return target; }
            set
            {
                if (target == value)
                    return;
                target = value;
                OnPropertyChanged(nameof(Target));
            }
        }

        [DisplayName("Destination Table")]
        public DBTable TargetTable
        {
            get { return _export?.Target?.Tables[target]; }
            set
            {
                target = value?.Name;
                OnPropertyChanged(nameof(TargetTable));
            }
        }

        [Browsable(false)]
        public bool Check
        {
            get { return check; }
            set
            {
                if (check == value)
                    return;
                check = value;
                OnPropertyChanged(nameof(Check));
            }
        }

        [DisplayName("Query")]
        public string Query
        {
            get { return query; }
            set
            {
                if (query == value)
                    return;
                query = value;
                OnPropertyChanged(nameof(Query));
            }
        }

        [Browsable(false)]
        public bool Complete
        {
            get { return complete; }
            set { complete = value; }
        }

        [Browsable(false)]
        public DBEColumnList Columns
        {
            get { return columns; }
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            DBETable ts = obj as DBETable;
            return string.Compare(this.source, ts.Source);
        }

        #endregion

        #region INotifyPropertyChanged implementation
        //[NonSerialized()]
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}

