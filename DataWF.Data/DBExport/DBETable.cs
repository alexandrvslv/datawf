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

