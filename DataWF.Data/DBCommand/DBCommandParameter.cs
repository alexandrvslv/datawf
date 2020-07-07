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
                case DBDataType.TimeSpan: DbType = DbType.Int64; break;
                case DBDataType.DateTime: DbType = DbType.DateTime; break;
                case DBDataType.TimeStamp: DbType = DbType.DateTime2; break;
                case DBDataType.Blob:
                case DBDataType.ByteArray:
                case DBDataType.ByteSerializable: DbType = DbType.Binary; break;
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
