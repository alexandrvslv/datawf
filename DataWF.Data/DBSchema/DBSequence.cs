/*
 SchemaInitialize.cs
 
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
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Xml.Serialization;
using DataWF.Common;

namespace DataWF.Data
{
    public class DBSequence : DBSchemaItem
    {
        private string cacheQuery;

        public DBSequence()
        { }

        public DBSequence(string name) : base(name)
        { }

        public long Current { get; set; } = 1;

        public int Increment { get; set; } = 1;

        [DefaultValue(DBDataType.Int)]
        public DBDataType DBDataType { get; set; } = DBDataType.Int;

        [DefaultValue(0)]
        public int Size { get; set; }

        [DefaultValue(0)]
        public int Scale { get; set; }

        public string GenerateQuery
        {
            get { return cacheQuery = cacheQuery ?? Schema.Connection.System.SequenceNextValue(this); }
        }

        public override object Clone()
        {
            return new DBSequence()
            {
                Name = name,
                Increment = Increment,
                DBDataType = DBDataType,
                Size = Size,
                Scale = Scale
            };
        }

        public override string FormatSql(DDLType ddlType)
        {
            var ddl = new StringBuilder();
            Schema?.Connection?.System.Format(ddl, this, ddlType);
            return ddl.ToString();
        }

        public long NextValue(DBTransaction transaction = null)
        {
            long result = 0;
            var temp = transaction ?? new DBTransaction(Schema?.Connection);
            try
            {
                Current = result = Convert.ToInt64(DBService.ExecuteQuery(temp, temp.AddCommand(GenerateQuery)));
                if (transaction == null)
                    temp.Commit();
            }
            finally
            {
                if (transaction == null)
                    temp.Dispose();
            }
            return result;
        }
    }
}
