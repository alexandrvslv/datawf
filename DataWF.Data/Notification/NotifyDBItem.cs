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
    public class DBItemBinarySerializer<T> where T : DBItem
    {
        public Dictionary<ushort, DBColumn> Map { get; set; }

        public object ConvertFromBinary(BinaryInvokerReader reader) => FromBinary(reader);

        public void ConvertToBinary(object value, BinaryInvokerWriter writer, bool writeToken) => ToBinary((T)value, writer, writeToken);

        public T FromBinary(BinaryInvokerReader reader)
        {
            throw new NotImplementedException();
        }

        public void ToBinary(T value, BinaryInvokerWriter writer, bool writeToken)
        {
            writer.WriteObjectBegin();
            if (Map == null)
            {
                var table = value.Table;
                Map = WriteMap(writer, table);
            }

            writer.WriteObjectEnd();
        }

        public static Dictionary<ushort, DBColumn> WriteMap(BinaryInvokerWriter writer, DBTable table)
        {
            var map = new Dictionary<ushort, DBColumn>();
            writer.WriteSchemaBegin();
            writer.WriteString(table.Name, false);
            ushort index = 0;
            foreach (var column in table.Columns)
            {
                if (column.ColumnType != DBColumnTypes.Default)
                    continue;
                writer.Writer.Write(index);
                writer.WriteString(column.Name, false);
                map[index++] = column;
            }
            writer.WriteSchemaEnd();
            return map;
        }
    }

    public class RelicateDBItem : IBinarySerializable
    {
        public DBLogType Command { get; set; }
        public int User { get; set; }
        public DBItem Value { get; set; }

        [XmlIgnore, JsonIgnore]
        public List<DBColumn> UpdateColumns { get; set; }

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

        public void Deserialize(BinaryInvokerReader reader)
        {
            reader.ReadToken();
            Command = (DBLogType)reader.Reader.ReadByte();
            User = reader.Reader.ReadInt32();
            reader.ReadToken();
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
            invokerWriter.Writer.Write((byte)Command);
            invokerWriter.Writer.Write((int)User);
            invokerWriter.WriteObjectEnd();
        }
    }

    public class NotifyDBItem : IBinarySerializable, IComparable<NotifyDBItem>
    {
        public DBLogType Command { get; set; }
        public int User { get; set; }
        public object Id { get; set; }

        [XmlIgnore, JsonIgnore]
        public DBItem Value { get; set; }

        public int CompareTo(NotifyDBItem other)
        {
            var res = ListHelper.Compare(Id, other.Id, null);
            return res != 0 ? res : Command.CompareTo(Command);
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

        public void Deserialize(BinaryInvokerReader reader)
        {
            reader.ReadToken();
            Command = (DBLogType)reader.Reader.ReadByte();
            User = reader.Reader.ReadInt32();
            Id = Helper.ReadBinary(reader.Reader);
            reader.ReadToken();
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
            invokerWriter.Writer.Write((byte)Command);
            invokerWriter.Writer.Write((int)User);
            Helper.WriteBinary(invokerWriter.Writer, Id, true);
            invokerWriter.WriteObjectEnd();
        }
    }
}
