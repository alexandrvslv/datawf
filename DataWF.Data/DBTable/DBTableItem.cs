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
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public abstract class DBTableItem : DBSchemaItem, IDBTableContent
    {
        private DBTable table;

        protected DBTableItem()
        { }

        protected DBTableItem(string name) : base(name)
        { }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public DBTable Table
        {
            get { return table; }
            set
            {
                if (table != value)
                {
                    table = value;
                    litem = null;
                }
            }
        }

        [XmlIgnore, JsonIgnore, Browsable(false)]
        public override DBSchema Schema
        {
            get { return Table?.Schema; }
        }

        public override string GetLocalizeCategory()
        {
            return Table?.FullName;
        }

        [Invoker(typeof(DBTableItem), nameof(DBTableItem.Table))]
        public class TableInvoker<T> : Invoker<T, DBTable> where T : DBTableItem
        {
            public static readonly TableInvoker<T> Instance = new TableInvoker<T>();
            public override string Name => nameof(DBTableItem.Table);

            public override bool CanWrite => true;

            public override DBTable GetValue(T target) => target.Table;

            public override void SetValue(T target, DBTable value) => target.Table = value;
        }
    }
}
