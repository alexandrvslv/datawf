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

namespace DataWF.Data
{
    public class DBItemSerializer<T> : ObjectSerializer<T> where T : DBItem
    {
        public static readonly DBItemSRSerializer<T> Instance = new DBItemSRSerializer<T>();

        public override T Read(BinaryInvokerReader reader, T value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            var token = reader.ReadToken();
            if (token == BinaryToken.Null)
            {
                return default(T);
            }
            var type = typeof(T);
            if (token == BinaryToken.ObjectBegin)
            {
                token = reader.ReadToken();
            }
            var table = DBTable.GetTable(typeof(T));

            if (token == BinaryToken.SchemaBegin)
            {
                map = ReadMap(reader, out type, out table);
                token = reader.ReadToken();
            }
            map = map ?? reader.GetMap(type);

            if (value == null)
            {
                value = (T)info.Constructor?.Create();
            }
            if (token == BinaryToken.ObjectEntry)
            {
                do
                {
                    reader.ReadProperty(value, map);
                }
                while (reader.ReadToken() == BinaryToken.ObjectEntry);
            }
            return value;
        }

        public void ReadProperty(BinaryInvokerReader reader, T element, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            var index = reader.ReadSchemaIndex();
            map.TryGetValue(index, out var property);

            if (property != null)
            {
                property.Read(reader, element, null);
            }
            else
            {
                var value = reader.Read(null, reader.Serializer.GetTypeInfo(property.DataType));
            }
        }

        public override void Write(BinaryInvokerWriter writer, T value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            writer.WriteObjectBegin();
            var valueType = value.GetType();

            if (map == null || valueType != typeof(T))
            {
                map = WriteMap(writer, valueType, value.Table);
            }
            foreach (var entry in map)
            {
                var property = entry.Value;
                WriteProperty(writer, property, value, entry.Key);
            }
            writer.WriteObjectEnd();
        }

        public void WriteProperty(BinaryInvokerWriter writer, IPropertySerializeInfo property, T element, ushort index)
        {
            writer.WriteObjectEntry();
            writer.WriteSchemaIndex(index);
            property.Write(writer, element);
        }

        public Dictionary<ushort, IPropertySerializeInfo> WriteMap(BinaryInvokerWriter writer, Type type, DBTable table)
        {
            writer.WriteSchemaBegin();
            writer.WriteSchemaName(type.Name);
            var map = writer.GetMap(type);
            if (map == null)
            {
                map = new Dictionary<ushort, IPropertySerializeInfo>();
                ushort index = 0;
                foreach (var column in GetColumns(table))
                {
                    writer.WriteSchemaEntry(index);
                    writer.WriteString(column.PropertyName ?? column.Name, false);
                    map[index++] = column;
                }
                writer.SetMap(type, map);
            }
            writer.WriteSchemaEnd();
            return map;
        }

        public virtual IEnumerable<DBColumn> GetColumns(DBTable table)
        {
            foreach (var column in table.Columns)
            {
                if (column.ColumnType != DBColumnTypes.Default)
                    continue;
                yield return column;
            }
        }

        public Dictionary<ushort, IPropertySerializeInfo> ReadMap(BinaryInvokerReader reader, out Type type, out DBTable table)
        {
            var token = reader.ReadToken();
            if (token == BinaryToken.SchemaBegin)
            {
                token = reader.ReadToken();
            }
            type = null;
            table = null;
            if (token == BinaryToken.SchemaName
                || token == BinaryToken.String)
            {
                var name = StringSerializer.Instance.Read(reader.Reader);
                type = TypeHelper.ParseType(name);
                table = DBTable.GetTable(type);
                token = reader.ReadToken();
            }
            var map = reader.GetMap(type);
            if (map == null)
            {
                map = new Dictionary<ushort, IPropertySerializeInfo>();
            }
            if (token == BinaryToken.SchemaEntry)
            {
                do
                {
                    var index = reader.ReadSchemaIndex();
                    var propertyName = reader.ReadString();
                    map[index] = table.ParseProperty(propertyName) ?? table.ParseColumn(propertyName);
                }
                while (reader.ReadToken() == BinaryToken.SchemaEntry);
                reader.SetMap(type, map);
            }

            return map;
        }
    }

    public class DBItemSRSerializer<T> : DBItemSerializer<T> where T : DBItem
    {
        public override IEnumerable<DBColumn> GetColumns(DBTable table)
        {
            foreach (var column in table.Columns)
            {
                if (column.ColumnType != DBColumnTypes.Default
                    || (column.Keys & DBColumnKeys.NoReplicate) == DBColumnKeys.NoReplicate)
                    continue;
                yield return column;
            }
        }
    }

    public class DBItemSerializer : DBItemSerializer<DBItem>
    { }
}
