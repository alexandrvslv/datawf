/*
 DBEColumn.cs
 
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
using DataWF.Common;

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

        [DisplayName("Source Code"), ReadOnly(true)]
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
                source = value == null ? null : value.Name;
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
            get { return table == null || table.TargetTable == null ? null : table.TargetTable.Columns[target]; }
            set
            {
                if (TargetColumn == value)
                    return;
                target = value == null ? null : value.Name;
                OnPropertyChanged(nameof(TargetColumn));
            }
        }

        public int Scale
        {
            get { return this.scale; }
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

        [DisplayName("User Defined"), ReadOnly(true)]
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

