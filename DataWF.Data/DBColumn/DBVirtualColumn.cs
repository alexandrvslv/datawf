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
using System.Reflection;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

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

        [XmlIgnore, JsonIgnore]
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