/*
 DBCommandParameters.cs
 
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

namespace DataWF.Data
{
    public class DBCommandParameter : IComparable
    {
        public DBCommandParameter()
        {
        }

        public DBCommandParameter(IDbDataParameter param, int index) : this(param.ParameterName)
        {
            ColumnName = param.SourceColumn;
            Direction = param.Direction;
            Order = index;
            DbType = param.DbType;
            Size = param.Size;
            Scale = param.Scale;
            if (param.GetType().FullName == "Oracle.ManagedDataAccess.Client.OracleParameter")
            {
                //newParam.OracleDbType = ((Oracle.DataAccess.Client.OracleParameter)param).OracleDbType;
            }
        }

        public DBCommandParameter(DBColumn column, string prefix)
        {
            Name = prefix + column.Name;
            Column = column;
            ColumnName = column.Name;
            Size = column.Size;
            Scale = (short)column.Scale;

            Direction = ParameterDirection.InputOutput;
            switch (column.DBDataType)
            {
                case DBDataType.Float: DbType = DbType.Single; break;
                case DBDataType.Double: DbType = DbType.Double; break;
                case DBDataType.Decimal: DbType = DbType.Decimal; break;
                case DBDataType.TinyInt: DbType = DbType.Byte; break;
                case DBDataType.ShortInt: DbType = DbType.Int16; break;
                case DBDataType.Int: DbType = DbType.Int32; break;
                case DBDataType.BigInt: DbType = DbType.Int64; break;
                case DBDataType.TimeSpan: DbType = DbType.DateTimeOffset; break;
                case DBDataType.DateTime: DbType = DbType.DateTime; break;
                case DBDataType.TimeStamp: DbType = DbType.DateTime2; break;
                case DBDataType.Blob:
                case DBDataType.ByteArray: DbType = DbType.Binary; break;
                case DBDataType.String: DbType = DbType.StringFixedLength; break;
                case DBDataType.Clob: DbType = DbType.String; break;
            }
        }

        public DBCommandParameter(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public DbType DbType { get; set; } = 0;

        public int Size { get; set; }

        public short Scale { get; set; }

        public ParameterDirection Direction { get; set; } = ParameterDirection.InputOutput;

        public string ColumnName { get; set; }

        public int Order { get; set; }

        public DBColumn Column { get; private set; }

        #region IComparable Members

        int IComparable.CompareTo(object obj)
        {
            return Order.CompareTo(((DBCommandParameter)obj).Order);
        }

        #endregion
        //public IDataParameter Temp { get { return temp; } set { temp = value; } }
    }
}
