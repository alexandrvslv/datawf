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
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class NotifyDBTable : IByteSerializable, IComparable<NotifyDBTable>
    {
        private DBTable table;

        [XmlIgnore, JsonIgnore]
        public DBTable Table
        {
            get => table ?? (table = DBTable.GetTable(Type));
            set => table = value;
        }

        public Type Type { get; set; }

        public List<NotifyDBItem> Items { get; set; } = new List<NotifyDBItem>();

        public int CompareTo(NotifyDBTable other)
        {
            return Table.CompareTo(other.Table);
        }

        public void Deserialize(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            using (var reader = new BinaryReader(stream))
            {
                Deserialize(reader);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            using (var invokerReader = new BinaryInvokerReader(reader))
            {
                Deserialize(invokerReader);
            }
        }

        public void Deserialize(BinaryInvokerReader invokerReader)
        {
            invokerReader.ReadToken();
            invokerReader.ReadToken();
            var name = invokerReader.ReadString();
            Type = TypeHelper.ParseType(name);
            invokerReader.ReadToken();
            invokerReader.ReadToken();
            while (invokerReader.ReadToken() == BinaryToken.ArrayEntry)
            {
                var item = new NotifyDBItem();
                item.Deserialize(invokerReader);
                Items.Add(item);
            }
            invokerReader.ReadToken();
        }

        public byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Serialize(writer);
                return stream.ToArray();
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            using (var invokerWriter = new BinaryInvokerWriter(writer))
            {
                Serialize(invokerWriter);
            }
        }

        public void Serialize(BinaryInvokerWriter invokerWriter)
        {
            invokerWriter.WriteObjectBegin();
            invokerWriter.WriteObjectEntry();
            invokerWriter.WriteString(TypeHelper.FormatBinary(Type), false);
            invokerWriter.WriteObjectEntry();
            invokerWriter.WriteArrayBegin();
            foreach (var item in Items)
            {
                invokerWriter.WriteArrayEntry();
                item.Serialize(invokerWriter);
            }
            invokerWriter.WriteArrayEnd();
            invokerWriter.WriteObjectEnd();
        }
    }
}
