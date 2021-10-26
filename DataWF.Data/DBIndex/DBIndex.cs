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
using System.Linq;
using System.Text;

[assembly: Invoker(typeof(DBIndex), nameof(DBIndex.Unique), typeof(DBIndex.UniqueInvoker<>))]
[assembly: Invoker(typeof(DBIndex), nameof(DBIndex.Columns), typeof(DBIndex.ColumnsInvoker<>))]
namespace DataWF.Data
{
    public class DBIndex : DBTableItem
    {
        private bool unique;

        public DBIndex()
        {
            Columns = new DBColumnReferenceList();
        }

        public bool Unique
        {
            get { return unique; }
            set
            {
                if (unique = value)
                    return;
                unique = value;
                OnPropertyChanged(nameof(Unique), DDLType.Alter);
            }
        }

        public DBColumnReferenceList Columns { get; set; }

        public override object Clone()
        {
            var index = new DBIndex()
            {
                Name = name,
                Unique = Unique
            };
            foreach (var column in Columns)
            {
                index.Columns.Add(column.Clone());
            }
            return index;
        }

        public override string FormatSql(DDLType ddlType)
        {
            var builder = new StringBuilder();
            Schema?.System?.Format(builder, this, ddlType);
            return builder.ToString();
        }

        public class UniqueInvoker<T> : Invoker<T, bool> where T : DBIndex
        {
            public static readonly UniqueInvoker<T> Instance = new UniqueInvoker<T>();
            public override string Name => nameof(DBIndex.Unique);

            public override bool CanWrite => true;

            public override bool GetValue(T target) => target.Unique;

            public override void SetValue(T target, bool value) => target.Unique = value;
        }

        public class ColumnsInvoker<T> : Invoker<T, DBColumnReferenceList> where T : DBIndex
        {
            public static readonly ColumnsInvoker<T> Instance = new ColumnsInvoker<T>();
            public override string Name => nameof(DBIndex.Columns);

            public override bool CanWrite => true;

            public override DBColumnReferenceList GetValue(T target) => target.Columns;

            public override void SetValue(T target, DBColumnReferenceList value) => target.Columns = value;
        }
    }
}
