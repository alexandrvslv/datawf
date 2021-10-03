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
using System.Data.Common;

namespace DataWF.Data
{
    public struct DBReaderEntry
    {
        public DBReaderEntry(DBTable table, string alias)
        {
            Alias = alias;
            Table = table;
            Columns = null;
            PrimaryKey = -1;
            StampKey = -1;
            ItemTypeKey = -1;
        }
        public string Alias;

        public DBTable Table;

        public List<(int Index, DBColumn Column)> Columns { get; set; }

        public int PrimaryKey;

        public int StampKey;

        public int ItemTypeKey;


        internal DBItem Load(DbDataReader reader, DBLoadParam loadParam)
        {
            lock (Table.Lock)
            {
                DBItem item;
                var typeIndex = ItemTypeKey > -1
                    ? reader.IsDBNull(ItemTypeKey) ? 0
                    : reader.GetInt32(ItemTypeKey) : 0;

                if (PrimaryKey > -1)
                {
                    if (reader.IsDBNull(PrimaryKey))
                        return null;
                    item = Table.PrimaryKey.GetOrCreate(reader, PrimaryKey, typeIndex);
                }
                else
                {
                    item = Table.NewItem(DBUpdateState.Default, false, typeIndex);
                }
                if (StampKey > -1 && !reader.IsDBNull(StampKey))
                {
                    var stamp = reader.GetDateTime(StampKey);
                    stamp = DateTime.SpecifyKind(stamp, DateTimeKind.Utc);

                    if (item.Stamp >= stamp)
                    {
                        return item;
                    }
                    else
                    {
                        Table.StampKey.SetValue(item, stamp, DBSetValueMode.Loading);
                    }
                }

                for (int i = 0; i < Columns.Count; i++)
                {
                    var columnIndex = Columns[i];
                    if (item.Attached && item.UpdateState != DBUpdateState.Default && columnIndex.Column.IsChanged(item))
                    {
                        continue;
                    }

                    columnIndex.Column.Read(reader, item, columnIndex.Index);
                }

                if (item != null
                    && !item.Attached
                    && (loadParam & DBLoadParam.NoAttach) != DBLoadParam.NoAttach)
                {
                    Table.Add(item);
                }
                return item;
            }
        }
    }
}

