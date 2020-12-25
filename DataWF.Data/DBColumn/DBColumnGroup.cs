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
using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public partial class DBColumnGroup : DBTableItem, IComparable, IComparable<DBColumnGroup>
    {
        protected int order = -1;

        public DBColumnGroup() : base()
        { }

        public DBColumnGroup(string name) : base(name)
        { }

        public override string FullName
        {
            get
            {
                return string.Format("{0}.{1}",
                Table == null ? string.Empty : Table.FullName,
                name);
            }
        }

        [Description("Порядковы номер в таблице"), DisplayName("Порядок"), Category("Отображение")]
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

        public IEnumerable<DBColumn> GetColumns()
        {
            return Table.Columns.GetByGroup(this);
        }

        public override int CompareTo(object obj)
        {
            return (CompareTo((DBColumnGroup)obj));
        }

        public int CompareTo(DBColumnGroup other)
        {
            return order.CompareTo(other.order);
        }

        public override object Clone()
        {
            return new DBColumnGroup()
            {
                name = name,
                order = order
            };
        }

        public override string FormatSql(DDLType ddlType, bool dependency = false)
        {
            return null;
        }
    }
}
