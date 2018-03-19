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
using DataWF.Common;

namespace DataWF.Data
{
    public class DBInformation
    {
        public static void LoadRefrences(DBTable table)
        {
            LoadRefrences(table.Schema, table);
        }

        public static void LoadRefrences(DBSchema schema, DBTable table = null)
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

            public static SMReference Generate(DBSchema schema, DBTable table)
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

            public void Parse(DBSchema schema, DBTable table)
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
                    var tab = table ?? DBService.ParseTable(item[iTable].ToString(), schema);
                    var col = tab?.ParseColumn(item[iName].ToString());
                    var rtab = DBService.ParseTable(item[iRefTable].ToString(), schema);
                    var rcol = rtab?.ParseColumn(item[iRefColumn].ToString());
                    if (col != null && rcol != null)
                    {
                        var reference = col.Table.Foreigns.GetByColumns(col, rcol);
                        if (reference == null)
                        {
                            reference = new DBForeignKey();
                            reference.Column = col;
                            reference.Reference = rcol;
                            col.Table.Foreigns.Add(reference);
                        }
                        reference.Name = name;
                    }
                }

            }
        }
    }


}
