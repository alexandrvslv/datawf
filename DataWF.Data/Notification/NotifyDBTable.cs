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
using System.IO;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

[assembly: Invoker(typeof(NotifyDBTable), nameof(NotifyDBTable.Type), typeof(NotifyDBTable.TypeInvoker))]
[assembly: Invoker(typeof(NotifyDBTable), nameof(NotifyDBTable.Items), typeof(NotifyDBTable.ItemsInvoker))]
namespace DataWF.Data
{
    public class NotifyDBTable : IComparable<NotifyDBTable>
    {
        private DBTable table;

        [XmlIgnore, JsonIgnore]
        public DBTable Table
        {
            get => table ?? (table = DBTable.GetTable(Type));
            set => table = value;
        }

        [ElementSerializer(typeof(TypeShortSerializer))]
        public Type Type { get; set; }

        public List<NotifyDBItem> Items { get; set; } = new List<NotifyDBItem>();

        public int CompareTo(NotifyDBTable other)
        {
            return Table.CompareTo(other.Table);
        }

        public class TypeInvoker : Invoker<NotifyDBTable, Type>
        {
            public override string Name => nameof(Type);

            public override bool CanWrite => true;

            public override Type GetValue(NotifyDBTable target) => target.Type;

            public override void SetValue(NotifyDBTable target, Type value) => target.Type = value;
        }

        public class ItemsInvoker : Invoker<NotifyDBTable, List<NotifyDBItem>>
        {
            public override string Name => nameof(Items);

            public override bool CanWrite => true;

            public override List<NotifyDBItem> GetValue(NotifyDBTable target) => target.Items;

            public override void SetValue(NotifyDBTable target, List<NotifyDBItem> value) => target.Items = value;
        }
    }
}
