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
                OnPropertyChanged(nameof(BaseName), true);
            }
        }

        [XmlIgnore, Category("Database")]
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
                if (value == BaseColumn)
                    return;
                BaseName = value?.Name;
                if (value != null)
                {
                    Name = value.Name;
                    Size = value.Size;
                    Scale = value.Scale;
                    IsReference = value.IsReference;
                    DBDataType = value.DBDataType;
                    GroupName = value.GroupName;
                    //Keys = value.Keys;
                }
                cacheBaseColumn = value;
            }
        }

        [XmlIgnore]
        public override Pull Pull
        {
            get { return BaseColumn?.Pull ?? null; }
            internal set { throw new Exception("No self pull on virtual column!"); }
        }

        public override string SqlName
        {
            get { return BaseName; }
        }
    }
}