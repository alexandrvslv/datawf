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
    public struct DBReader
    {
        private DBLoadProgressEventArgs arg;
        private bool isSingle;

        public DBReader(DBTransaction transaction,
            DBTable table,
            IQQuery query,
            DbDataReader reader,
            DBLoadParam param = DBLoadParam.None,
            int count = -1)
        {

            transaction.CurrentReader = reader;
            View = transaction.View;
            Table = table;
            Query = query;
            Reader = reader;
            Param = param;
            Count = count;
            Fields = null;
            arg = count > 0
                ? new DBLoadProgressEventArgs(transaction.View, Count, 0, null)
                : null;
            isSingle = false;
            CheckColumns();
        }

        public DBTable Table;

        public IQQuery Query;

        public DbDataReader Reader;

        public DBLoadParam Param;

        public List<DBReaderEntry> Fields;

        public int Count;

        public IDBTableView View;

        public DBItem Load()
        {
            var item = isSingle
                ? Fields[0].Load(Reader, Param)
                : LoadJoin();

            RaiseLoadProgress(item);

            return item;
        }

        internal DBItem LoadJoin()
        {
            DBItem item = null;
            foreach (var readerFields in Fields)
            {
                var dbItem = readerFields.Load(Reader, Param);
                if (item == null)
                    item = dbItem;
            }
            return item;
        }

        public void Dispose()
        {
            Reader?.Dispose();
        }

        internal void CheckColumns()
        {
            bool newcol = false;
            var fieldsCount = Reader.FieldCount;
            var readerFieldsList = new List<DBReaderEntry>(1);
            var fields = new DBReaderEntry(null, null);

            for (int i = 0; i < fieldsCount; i++)
            {
                string fieldName = Reader.GetName(i);
                if (fieldName.Length == 0)
                    fieldName = i.ToString();
                var alias = string.Empty;
                var table = Table;
                var dotIndex = fieldName.IndexOf('.');
                if (dotIndex > -1)
                {
                    alias = fieldName.Substring(0, dotIndex);
                    var tableIndex = fieldName.Substring(0, dotIndex);
                    fieldName = fieldName.Substring(dotIndex + 1);
                    if (int.TryParse(tableIndex, out int tableIntIndex))
                        table = (DBTable)Query.Tables[tableIntIndex].Table;
                    else
                        table = Table.Schema.GetTable(tableIndex) ?? Table;
                }

                var column = table.GetOrCreateColumn(fieldName, Reader.GetFieldType(i), ref newcol);

                if (fields.Table == null)
                {
                    fields.Alias = alias;
                    fields.Table = table;
                    fields.Columns = new List<(int Index, DBColumn Column)>(table.Columns.Count - 3);
                }
                else if (fields.Table != table
                    || !string.Equals(fields.Alias, alias, StringComparison.OrdinalIgnoreCase))
                {
                    readerFieldsList.Add(fields);
                    fields = new DBReaderEntry(table, alias)
                    {
                        Columns = new List<(int Index, DBColumn Column)>(table.Columns.Count - 3)
                    };
                }

                if (column.IsPrimaryKey)
                {
                    fields.PrimaryKey = i;
                }
                else if ((column.Keys & DBColumnKeys.Stamp) == DBColumnKeys.Stamp)
                {
                    fields.StampKey = i;
                }
                else if ((column.Keys & DBColumnKeys.ItemType) == DBColumnKeys.ItemType)
                {
                    fields.ItemTypeKey = i;
                }
                else
                {
                    fields.Columns.Add((i, column));
                }
            }
            readerFieldsList.Add(fields);
            if (newcol)
            {
                //RaiseLoadColumns(new DBLoadColumnsEventArgs(transaction.View));
            }
            Fields = readerFieldsList;
            isSingle = readerFieldsList.Count == 1;
        }

        internal void CloseReader()
        {
            if (!(Reader?.IsClosed ?? true))
            {
                Reader.Close();
            }
        }

        internal void RaiseLoadProgress(DBItem item)
        {
            if (Count > 0)
            {
                arg.Current++;
                arg.CurrentRow = item;
                Table.RaiseLoadProgress(arg);
            }

            if (View?.Table == item.Table
                && View.IsStatic)
                View.Add(item);
        }


    }
}

