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
using System.Collections.Generic;
using System.IO;
using DataWF.Common;

namespace DataWF.Data
{
    public static class DBItemBinarySerialize
    {
        public static void WriteSeparator(BinaryWriter writer, DBRowBinarySeparator sep)
        {
            byte[] bytes = BitConverter.GetBytes((int)sep);
            writer.Write(bytes, 0, 3);
        }

        public static DBRowBinarySeparator PeekSeparator(BinaryReader reader)
        {
            long pos = reader.BaseStream.Position;
            var bufer = reader.ReadBytes(3);
            var rez = DBRowBinarySeparator.None;
            if (bufer.Length == 3)
            {
                int b = BitConverter.ToInt32(new byte[] { bufer[0], bufer[1], bufer[2], 0 }, 0);
                switch (b)
                {
                    case (int)DBRowBinarySeparator.RowStart:
                        rez = DBRowBinarySeparator.RowStart;
                        break;
                    case (int)DBRowBinarySeparator.RowEnd:
                        rez = DBRowBinarySeparator.RowEnd;
                        break;
                    case (int)DBRowBinarySeparator.ColumnsStart:
                        rez = DBRowBinarySeparator.ColumnsStart;
                        break;
                    case (int)DBRowBinarySeparator.ColumnsEnd:
                        rez = DBRowBinarySeparator.ColumnsEnd;
                        break;
                    case (int)DBRowBinarySeparator.End:
                        rez = DBRowBinarySeparator.End;
                        break;
                    default:
                        reader.BaseStream.Position = pos;
                        break;
                }
            }
            else
                throw new IOException("Ошибка чтения записи");

            return rez;
        }

        public static void Read(byte[] data, DBItem row)
        {
            if (data == null || data.Length == 0)
                return;
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                Read(reader, row, ReadColumns(reader, row.Table));
            }
        }

        public static void Read(BinaryReader br, DBItem row, Dictionary<int, DBColumn> map)
        {
            while (true)
            {
                DBRowBinarySeparator separator = PeekSeparator(br);
                if (separator != DBRowBinarySeparator.RowStart &&
                    separator != DBRowBinarySeparator.None)
                    break;
                int columnIndex = br.ReadInt32();
                DBColumn dbColumn = map[columnIndex];
                object value = Helper.ReadBinary(br);
                row.SetValue(value, dbColumn, DBSetValueMode.Loading);
            }
            row.Accept((IUserIdentity)null);
        }

        public static Dictionary<int, DBColumn> ReadColumns(BinaryReader br, DBTable table)
        {
            var map = new Dictionary<int, DBColumn>(table.Columns.Count);
            while (true)
            {
                DBRowBinarySeparator separator = PeekSeparator(br);
                if (separator != DBRowBinarySeparator.ColumnsStart &&
                    separator != DBRowBinarySeparator.None)
                    break;

                string column = br.ReadString();
                int index = br.ReadInt32();
                map.Add(index, table.ParseColumnProperty(column));
            }
            return map;
        }

        public static Dictionary<DBColumn, int> WriteColumns(BinaryWriter bw, DBTable tble)
        {
            return WriteColumns(bw, tble.Columns);
        }

        public static Dictionary<DBColumn, int> WriteColumns(BinaryWriter bw, ICollection<DBColumn> columns)
        {
            var map = new Dictionary<DBColumn, int>(columns.Count);
            int index = 100;
            WriteSeparator(bw, DBRowBinarySeparator.ColumnsStart);
            foreach (DBColumn column in columns)
            {
                if (column.ColumnType != DBColumnTypes.Default)
                    continue;
                index++;
                map.Add(column, index);
                bw.Write(column.Property ?? column.Name);
                bw.Write(index);
            }
            WriteSeparator(bw, DBRowBinarySeparator.ColumnsEnd);
            return map;
        }

        public static void Write(BinaryWriter writer, DBItem row, Dictionary<DBColumn, int> map, bool old = false)
        {
            WriteSeparator(writer, DBRowBinarySeparator.RowStart);
            foreach (KeyValuePair<DBColumn, int> item in map)
            {
                object field = row.GetValue(item.Key);
                object value = old && row.GetOld(item.Key, out object oldValue) ? oldValue : field;
                if (value == null)
                    continue;
                writer.Write(item.Value);
                Helper.WriteBinary(writer, value, true);
            }
            WriteSeparator(writer, DBRowBinarySeparator.RowEnd);
        }       

        public static byte[] Write(DBItem row, bool old = false)
        {
            return Write(row, row.Table.Columns, old);
        }

        public static byte[] Write(DBItem row, ICollection<DBColumn> columns, bool old = false)
        {
            if (row == null)
                return null;
            byte[] buf = null;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Write(writer, row, WriteColumns(writer, columns), old);
                WriteSeparator(writer, DBRowBinarySeparator.End);
                writer.Flush();
                buf = stream.ToArray();
            }
            return buf;
        }

        //public static byte[] WriteMap(Dictionary<string, object> row, ICollection<DBColumn> columns)
        //{
        //    if (row == null)
        //        return null;
        //    byte[] buf = null;
        //    using (MemoryStream ms = new MemoryStream())
        //    using (BinaryWriter bw = new BinaryWriter(ms))
        //    {
        //        WriteMap(bw, row, WriteDBColumns(bw, columns));
        //        WriteSeparator(bw, DBRowBinarySeparator.End);
        //        bw.Flush();
        //        buf = ms.ToArray();
        //    }
        //    return buf;
        //}
    }


}
