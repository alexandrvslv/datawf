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
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBLogColumn : DBColumn
    {
        public static string GetName(DBColumn column)
        {
            return column.Name + "_log";
        }

        private DBColumn baseColumn;

        public DBLogColumn()
        { }

        public DBLogColumn(DBColumn column)
        {
            BaseColumn = column;
        }

        public IDBLogTable LogTable => (IDBLogTable)Table;

        [Browsable(false)]
        public string BaseName { get; set; }

        [XmlIgnore, JsonIgnore]
        public DBColumn BaseColumn
        {
            get { return baseColumn ?? (baseColumn = LogTable?.BaseTable?.ParseColumn(BaseName)); }
            set
            {
                if (value == null)
                {
                    throw new Exception("BaseColumn value is empty!");
                }

                baseColumn = value;
                BaseName = value.Name;
                Name = GetName(value);
                DisplayName = value.DisplayName + " Log";
                DBDataType = value.DBDataType;
                DataType = value.DataType;
                ReferenceTable = value.ReferenceTable;
                Size = value.Size;
                Scale = value.Scale;
                if (value.IsFile)
                {
                    Keys |= DBColumnKeys.File;
                }
                if (value.IsFileName)
                {
                    Keys |= DBColumnKeys.FileName;
                }
                if (value.IsFileLOB)
                {
                    Keys |= DBColumnKeys.FileLOB;
                }
                if (value.IsTypeKey)
                {
                    Keys |= DBColumnKeys.ItemType;
                }
                if ((value.Keys & DBColumnKeys.Access) == DBColumnKeys.Access)
                {
                    Keys |= DBColumnKeys.Access;
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public override AccessValue Access
        {
            get { return BaseColumn?.Access; }
            set { base.Access = value; }
        }

        [Invoker(typeof(DBLogColumn), nameof(DBLogColumn.BaseName))]
        public class BaseNameInvoker<T> : Invoker<T, string> where T : DBLogColumn
        {
            public static readonly BaseNameInvoker<T> Instance = new BaseNameInvoker<T>();
            public override string Name => nameof(DBLogColumn.BaseName);

            public override bool CanWrite => true;

            public override string GetValue(T target) => target.BaseName;

            public override void SetValue(T target, string value) => target.BaseName = value;
        }

        [Invoker(typeof(DBLogColumn), nameof(DBLogColumn.BaseColumn))]
        public class BaseColumnInvoker<T> : Invoker<T, DBColumn> where T : DBLogColumn
        {
            public override string Name => nameof(DBLogColumn.BaseColumn);

            public override bool CanWrite => true;

            public override DBColumn GetValue(T target) => target.BaseColumn;

            public override void SetValue(T target, DBColumn value) => target.BaseColumn = value;
        }

        [Invoker(typeof(DBLogColumn), nameof(LogTable))]
        public class LogTableInvoker<T> : Invoker<T, IDBLogTable> where T : DBLogColumn
        {
            public override string Name => nameof(DBLogColumn.LogTable);

            public override bool CanWrite => false;

            public override IDBLogTable GetValue(T target) => target.LogTable;

            public override void SetValue(T target, IDBLogTable value) { }
        }
    }
}
