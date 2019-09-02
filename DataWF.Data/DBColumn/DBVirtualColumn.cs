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
using System.Reflection;
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
                BaseName = value?.Name;
                cacheBaseColumn = value;
                if (value != null)
                {
                    if (string.IsNullOrEmpty(Name))
                    {
                        Name = value.Name;
                    }
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override string CultureCode { get => BaseColumn.CultureCode; set => base.CultureCode = value; }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override string Property { get => BaseColumn.Property; set => base.Property = value; }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override string ReferenceProperty { get => BaseColumn.ReferenceProperty; set => base.Property = value; }

        [XmlIgnore, JsonIgnore]
        public override DBColumnKeys Keys { get => BaseColumn.Keys; set => base.Keys = value; }

        [XmlIgnore, JsonIgnore]
        public override int Size { get => BaseColumn.Size; set => base.Size = value; }

        [XmlIgnore, JsonIgnore]
        public override int Scale { get => BaseColumn.Scale; set => base.Scale = value; }

        [XmlIgnore, JsonIgnore, ReadOnly(true)]
        public override Type DataType { get => BaseColumn.DataType; set => base.DataType = value; }

        [XmlIgnore, JsonIgnore]
        public override DBDataType DBDataType { get => BaseColumn.DBDataType; set => base.DBDataType = value; }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override string GroupName { get => BaseColumn.GroupName; set => base.GroupName = value; }

        [XmlIgnore, JsonIgnore]
        public override DBColumnTypes ColumnType { get => BaseColumn.ColumnType; set => base.ColumnType = value; }

        [XmlIgnore, JsonIgnore]
        public override PropertyInfo PropertyInfo { get => BaseColumn.PropertyInfo; set => base.PropertyInfo = value; }
        [XmlIgnore, JsonIgnore]
        public override PropertyInfo ReferencePropertyInfo { get => BaseColumn.ReferencePropertyInfo; set => base.ReferencePropertyInfo = value; }
        [XmlIgnore, JsonIgnore]
        public override IInvoker PropertyInvoker { get => BaseColumn.PropertyInvoker; set => base.PropertyInvoker = value; }
        [XmlIgnore, JsonIgnore]
        public override IInvoker ReferencePropertyInvoker { get => BaseColumn.ReferencePropertyInvoker; set => base.ReferencePropertyInvoker = value; }

        protected internal override void CheckPull()
        {
            Pull = BaseColumn?.Pull;
        }

        public override string SqlName
        {
            get { return BaseName; }
        }

        public override bool GetOld(int hindex, out object obj)
        {
            return BaseColumn.GetOld(hindex, out obj);
        }

        public override void RemoveOld(int hindex)
        {
            BaseColumn.RemoveOld(hindex);
        }

        public override void SetOld(int hindex, object value)
        {
            BaseColumn.SetOld(hindex, value);
        }

        //public override object GetTag(int hindex)
        //{
        //    return BaseColumn.GetTag(hindex);
        //}

        //public override void RemoveTag(int hindex)
        //{
        //    BaseColumn.RemoveTag(hindex);
        //}

        //public override void SetTag(int hindex, object value)
        //{
        //    BaseColumn.SetTag(hindex, value);
        //}
    }
}