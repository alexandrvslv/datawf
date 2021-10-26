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
    public class DBEColumn : IComparable, INotifyPropertyChanged, ICheck
    {
        [NonSerialized]
        private DBETable table;
        private DBDataType type;
        private int order;
        private string source = "";
        private string target = "";
        private bool check = true;
        private bool user;
        private bool primaryKey;
        private int size = -1;
        private int scale = -1;
        private string defaultValue;

        public DBEColumn()
        {
        }

        public DBEColumn(string name)
        {
            this.source = name;
            this.target = name;
        }

        public override string ToString()
        {
            return source + " -> " + target;
        }

        [Browsable(false)]
        public DBETable Table
        {
            get { return table; }
            set { table = value; }
        }


        [DisplayName("Data Type")]
        public DBDataType DataType
        {
            get { return this.type; }
            set
            {
                if (type == value)
                    return;
                type = value;
                OnPropertyChanged(nameof(DataType));
            }
        }

        [DisplayName("Order")]
        public int Order
        {
            get { return order; }
            set
            {
                if (order == value)
                    return;
                order = value;
                OnPropertyChanged(nameof(Order));
            }
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

        [DisplayName("Source Column")]
        public DBColumn SourceColumn
        {
            get { return table == null || table.SourceTable == null ? null : table.SourceTable.Columns[source]; }
            set
            {
                if (SourceColumn == value)
                    return;
                source = value?.Name;
                OnPropertyChanged(nameof(SourceColumn));
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

        [DisplayName("Destination Column")]
        public DBColumn TargetColumn
        {
            get { return table?.TargetTable?.Columns[target]; }
            set
            {
                if (TargetColumn == value)
                    return;
                target = value?.Name;
                OnPropertyChanged(nameof(TargetColumn));
            }
        }

        public int Scale
        {
            get { return scale; }
            set
            {
                if (scale == value)
                    return;
                scale = value;
                OnPropertyChanged(nameof(Scale));
            }
        }

        public int Size
        {
            get { return this.size; }
            set
            {
                if (size == value)
                    return;
                size = value;
                OnPropertyChanged(nameof(Size));
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

        [DisplayName("User Defined")]
        public bool UserDefined
        {
            get { return user; }
            set
            {
                if (user == value)
                    return;
                user = value;
                OnPropertyChanged(nameof(UserDefined));
            }
        }

        [DisplayName("Is Primary Key")]
        public bool PrimaryKey
        {
            get { return primaryKey; }
            set
            {
                if (primaryKey == value)
                    return;
                primaryKey = value;
                OnPropertyChanged(nameof(PrimaryKey));
            }
        }

        [DisplayName("Default Value")]
        public string DefaultValue
        {
            get { return defaultValue; }
            set
            {
                if (defaultValue == value)
                    return;
                defaultValue = value;
                OnPropertyChanged(nameof(DefaultValue));
            }
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            DBEColumn s = obj as DBEColumn;
            return this.order.CompareTo(s.order);
        }

        #endregion

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }

}

