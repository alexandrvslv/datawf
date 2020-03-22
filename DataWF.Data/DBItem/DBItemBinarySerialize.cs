/*
 DBService.cs
 
 Author:
      Alexandr <alexandr_vslv@mail.ru> 

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU Lesser General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU Lesser General Public License for more details.

 You should have received a copy of the GNU Lesser General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
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

        public static void Read(BinaryReader br, DBItem row, Dictionary<int, string> map)
        {
            while (true)
            {
                DBRowBinarySeparator separator = PeekSeparator(br);
                if (separator != DBRowBinarySeparator.RowStart &&
                    separator != DBRowBinarySeparator.None)
                    break;
                int column = br.ReadInt32();
                object value = Helper.ReadBinary(br);
                DBColumn dbColumn = row.Table.ParseColumn(map[column]);
                row.SetValue(value, dbColumn, DBSetValueMode.Loading);
            }
            row.Accept((IUserIdentity)null);
        }

        public static void ReadMap(byte[] data, DBTable table, Dictionary<string, object> row)
        {
            if (data == null || data.Length == 0)
                return;
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                ReadMap(reader, row, ReadColumns(reader, table));
            }

        }
        public static void ReadMap(BinaryReader br, Dictionary<string, object> row, Dictionary<int, string> map)
        {
            while (true)
            {
                DBRowBinarySeparator separator = PeekSeparator(br);
                if (separator != DBRowBinarySeparator.RowStart &&
                    separator != DBRowBinarySeparator.None)
                    break;
                int column = br.ReadInt32();
                object value = Helper.ReadBinary(br);
                row[map[column]] = value;
            }
        }

        public static Dictionary<int, string> ReadColumns(BinaryReader br, DBTable table)
        {
            Dictionary<int, string> map = new Dictionary<int, string>(table.Columns.Count);
            while (true)
            {
                DBRowBinarySeparator separator = PeekSeparator(br);
                if (separator != DBRowBinarySeparator.ColumnsStart &&
                    separator != DBRowBinarySeparator.None)
                    break;

                string column = br.ReadString();
                int index = br.ReadInt32();
                map.Add(index, column);
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
                bw.Write(column.Name);
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

        public static void WriteMap(BinaryWriter writer, Dictionary<string, object> row, Dictionary<DBColumn, int> map)
        {
            WriteSeparator(writer, DBRowBinarySeparator.RowStart);
            foreach (KeyValuePair<DBColumn, int> item in map)
            {
                object value = row[item.Key.Name];
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
