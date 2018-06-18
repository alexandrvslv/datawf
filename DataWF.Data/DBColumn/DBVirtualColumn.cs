/*
 DBColumn.cs
 
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
using System.Xml.Serialization;
using DataWF.Common;
using Newtonsoft.Json;

namespace DataWF.Data
{
    public class DBVirtualColumn : DBColumn
    {
        private DBColumn cacheBaseColumn;
        protected string bname;

        public DBVirtualColumn()
        { }

        public DBVirtualColumn(string name) : base(name)
        { }

        public DBVirtualColumn(DBColumn baseColumn)
        {
            BaseColumn = baseColumn;
        }

        public IDBVirtualTable VirtualTable
        {
            get { return (IDBVirtualTable)Table; }
        }

        [Browsable(false), Category("Database")]
        public string BaseName
        {
            get { return bname; }
            set
            {
                if (bname == value)
                    return;
                bname = value;
                cacheBaseColumn = null;
                OnPropertyChanged(nameof(BaseName), DDLType.Alter);
            }
        }

        [XmlIgnore, JsonIgnore, Category("Database")]
        public DBColumn BaseColumn
        {
            get
            {
                if (cacheBaseColumn == null && bname != null)
                    cacheBaseColumn = VirtualTable?.BaseTable?.Columns[bname];
                return cacheBaseColumn;
            }
            set
            {
                cacheBaseColumn = value;
                BaseName = value?.Name;
                if (value != null)
                {
                    if (string.IsNullOrEmpty(Name))
                    {
                        Name = value.Name;
                    }
                    Size = value.Size;
                    Scale = value.Scale;
                    IsReference = value.IsReference;
                    DataType = value.DataType;
                    GroupName = value.GroupName;
                    ColumnType = value.ColumnType;
                    //Keys = value.Keys;
                }
            }
        }

        protected internal override void CheckPull()
        {
            Pull = BaseColumn?.Pull;
        }

        public override string SqlName
        {
            get { return BaseName; }
        }
    }
}