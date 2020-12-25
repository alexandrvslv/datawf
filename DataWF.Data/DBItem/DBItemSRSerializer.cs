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
using System.Linq;
using System.Reflection;

namespace DataWF.Data
{
    public class DBItemSerializer<T> : ObjectSerializer<T> where T : DBItem
    {
        public DBItemSerializer(DBTable table)
        {
            Table = table;
        }

        public DBTable Table { get; set; }

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
            if (token == BinaryToken.SchemaBegin)
            {
                map = ReadMap(reader, out type);
                token = reader.ReadToken();
            }
            map = map ?? reader.GetMap(type);

            if (value == null)
            {
                value = (T)Table.NewItem(DBUpdateState.Insert, false, type);
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

        public override void Write(BinaryInvokerWriter writer, T value, TypeSerializeInfo info, Dictionary<ushort, IPropertySerializeInfo> map)
        {
            writer.WriteObjectBegin();
            var valueType = value.GetType();

            if (map == null || valueType != typeof(T))
            {
                map = WriteMap(writer, valueType);
            }
            foreach (var entry in map)
            {
                var property = entry.Value;
                writer.WriteProperty(property, value, entry.Key);
            }
            writer.WriteObjectEnd();
        }

        public Dictionary<ushort, IPropertySerializeInfo> WriteMap(BinaryInvokerWriter writer, Type type)
        {
            writer.WriteSchemaBegin();
            writer.WriteSchemaName(type.Name);
            var map = writer.GetMap(type);
            if (map == null)
            {
                map = new Dictionary<ushort, IPropertySerializeInfo>();
                ushort index = 0;
                foreach (var column in GetColumns(Table))
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

        public Dictionary<ushort, IPropertySerializeInfo> ReadMap(BinaryInvokerReader reader, out Type type)
        {
            var token = reader.ReadToken();
            if (token == BinaryToken.SchemaBegin)
            {
                token = reader.ReadToken();
            }
            type = null;
            if (token == BinaryToken.SchemaName
                || token == BinaryToken.String)
            {
                var name = StringSerializer.Instance.Read(reader.Reader);
                type = TypeHelper.ParseType(name);
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
                    map[index] = Table.ParseProperty(propertyName) ?? Table.ParseColumn(propertyName);
                }
                while (reader.ReadToken() == BinaryToken.SchemaEntry);
                reader.SetMap(type, map);
            }

            return map;
        }
    }

    public class DBItemSRSerializer<T> : DBItemSerializer<T> where T : DBItem
    {
        public DBItemSRSerializer(DBTable table) : base(table)
        { }

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
    {
        public DBItemSerializer(DBTable table) : base(table)
        { }
    }
}
