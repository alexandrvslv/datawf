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

namespace DataWF.Data
{
    public class DBInformation
    {
        public static void LoadRefrences(DBTable table)
        {
            LoadRefrences(table.Schema, table);
        }

        public static void LoadRefrences(IDBSchema schema, DBTable table = null)
        {
            var info = SMReference.Generate(schema, table);
            info.Parse(schema, table);
        }

        public class SchemaMap
        {
            public string SourceTable { get; set; }
            public string Schema { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string Filter { get; set; }
        }

        public class SMReference : SchemaMap
        {
            public string Table { get; set; }
            public string Column { get; set; }
            public string ReferenceSchema { get; set; }
            public string ReferenceTable { get; set; }
            public string ReferenceColumn { get; set; }

            public static SMReference Generate(IDBSchema schema, DBTable table)
            {
                var info = new SMReference
                {
                    SourceTable = "",
                    Name = "constraint_name",
                    Schema = "schema_name",
                    Table = "table_name",
                    Column = "column_name",
                    ReferenceSchema = "reference_schema_name",
                    ReferenceTable = "reference_table_name",
                    ReferenceColumn = "reference_column_name",
                    Filter = @"SELECT  
     KCU1.CONSTRAINT_NAME AS constraint_name 
    ,KCU1.CONSTRAINT_SCHEMA AS schema_name 
    ,KCU1.TABLE_NAME AS table_name 
    ,KCU1.COLUMN_NAME AS column_name 
    ,KCU2.CONSTRAINT_NAME AS reference_constraint_name 
    ,KCU1.CONSTRAINT_SCHEMA AS reference_schema
    ,KCU2.TABLE_NAME AS reference_table_name 
    ,KCU2.COLUMN_NAME AS reference_column_name     
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS RC 

INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU1 
    ON KCU1.CONSTRAINT_CATALOG = RC.CONSTRAINT_CATALOG  
    AND KCU1.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA 
    AND KCU1.CONSTRAINT_NAME = RC.CONSTRAINT_NAME 

INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KCU2 
    ON KCU2.CONSTRAINT_CATALOG = RC.UNIQUE_CONSTRAINT_CATALOG  
    AND KCU2.CONSTRAINT_SCHEMA = RC.UNIQUE_CONSTRAINT_SCHEMA 
    AND KCU2.CONSTRAINT_NAME = RC.UNIQUE_CONSTRAINT_NAME 
    AND KCU2.ORDINAL_POSITION = KCU1.ORDINAL_POSITION "
                };
                if (schema.Connection.Schema != null || table != null)
                {
                    var schemaFilter = schema.Connection.Schema == null ? null : string.Format("RC.CONSTRAINT_SCHEMA='{0}'", schema.Connection.Schema);
                    var tableFilter = table == null ? null : string.Format("KCU1.TABLE_NAME='{0}'", table.Name);
                    info.Filter += string.Format("\nWHERE {0}{1}{2}", schemaFilter, schemaFilter != null && tableFilter != null ? " AND " : " ", tableFilter);
                }
                if (schema.Connection.System == DBSystem.SQLite)
                {
                    info.Filter = null;//TODO
                }
                if (schema.Connection.System == DBSystem.Oracle)
                {
                    info.Filter = @"SELECT 
    a.constraint_name, 
    c.owner as schema_name,  
    a.table_name, 
    a.column_name, 
    c_pk.constraint_name as reference_constraint_name
    c.r_owner as reference_schema, 
    c_pk.table_name as rererence_table_name, 
    c_pk.column_name as rererence_column_name
  FROM all_cons_columns a
  JOIN all_constraints c ON a.owner = c.owner
                        AND a.constraint_name = c.constraint_name
  JOIN all_constraints c_pk ON c.r_owner = c_pk.owner
                           AND c.r_constraint_name = c_pk.constraint_name
 WHERE c.constraint_type = 'R'";
                }
                return info;
            }

            public void Parse(IDBSchema schema, DBTable table)
            {
                QResult list = schema.Connection.ExecuteQResult(Filter);

                int iName = list.GetIndex(Name);
                int iSchema = list.GetIndex(Schema);
                int iTable = list.GetIndex(Table);
                int iColumn = list.GetIndex(Column);
                int iRefSchema = list.GetIndex(ReferenceSchema);
                int iRefTable = list.GetIndex(ReferenceTable);
                int iRefColumn = list.GetIndex(ReferenceColumn);
                foreach (object[] item in list.Values)
                {
                    var name = item[iName].ToString();
                    var tab = table ?? schema.GetTable(item[iTable].ToString());
                    var col = tab?.GetColumn(item[iName].ToString());
                    var rtab = schema.GetTable(item[iRefTable].ToString());
                    var rcol = rtab?.GetColumn(item[iRefColumn].ToString());
                    if (col != null && rcol != null)
                    {
                        var reference = col.Table.Foreigns.GetByColumns(col, rcol);
                        if (reference == null)
                        {
                            reference = new DBForeignKey
                            {
                                Column = col,
                                Reference = rcol
                            };
                            col.Table.Foreigns.Add(reference);
                        }
                        reference.Name = name;
                    }
                }

            }
        }
    }


}
