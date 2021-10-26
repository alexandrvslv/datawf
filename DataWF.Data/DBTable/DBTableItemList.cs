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
using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBTableItemList<T> : DBSchemaItemList<T> where T : DBTableItem, new()
    {

        public DBTableItemList(DBTable table) : base()
        {
            Table = table;
        }

        [XmlIgnore, JsonIgnore]
        public override DBSchema Schema
        {
            get { return base.Schema ?? Table?.Schema; }
            internal set { base.Schema = value; }
        }

        [XmlIgnore, JsonIgnore]
        public DBTable Table { get; set; }

        public override int AddInternal(T item)
        {
            if (Table == null)
            {
                throw new InvalidOperationException("Table property nead to be specified before add any item!");
            }
            if (item.Table == null)
            {
                item.Table = Table;
            }
            return base.AddInternal(item);
        }

        public override void Dispose()
        {
            Table = null;
            base.Dispose();
        }
    }
}
