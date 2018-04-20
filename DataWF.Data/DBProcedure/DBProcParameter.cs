/*
 ProcedureParameter.cs
 
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
using System.Data;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;

namespace DataWF.Data
{

    public class DBProcParameter : DBSchemaItem//, IComparable
    {
        [NonSerialized]
        private DBColumn cacheColumn;

        public DBProcParameter()
        { }

        public DBProcedure Procedure { get; set; }

        public string DataTypeName { get; set; }

        [XmlIgnore]
        public Type DataType
        {
            get { return DataTypeName.Length == 0 ? typeof(string) : TypeHelper.ParseType(DataTypeName); }
            set { DataTypeName = TypeHelper.BinaryFormatType(value); }
        }

        public ParameterDirection Direction { get; set; }

        public string ColumnName { get; set; }

        [XmlIgnore]
        public DBColumn Column
        {
            get
            {
                if (cacheColumn == null)
                {
                    cacheColumn = DBService.ParseColumn(ColumnName);
                }
                return cacheColumn as DBColumn;
            }
            set
            {
                ColumnName = value == null ? null : value.FullName;
                if (value != null)
                {
                    DataType = value.DataType;
                    Name = value.Name;
                }
            }
        }

        public override object Clone()
        {
            return new DBProcParameter()
            {
                Name = Name,
                DataTypeName = DataTypeName,
                ColumnName = ColumnName
            };
        }

        public override string FormatSql(DDLType ddlType)
        {
            return null;
        }
    }
}
