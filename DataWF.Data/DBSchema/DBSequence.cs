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
using DataWF.Common;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading;

namespace DataWF.Data
{
    public class DBSequence : DBSchemaItem
    {
        private string cacheQuery;
        private long current = 1;
        private int changed = 0;

        public DBSequence()
        { }

        public DBSequence(string name) : base(name)
        { }

        public long Current
        {
            get => current;
            set
            {
                Interlocked.CompareExchange(ref current, value, current);
            }
        }

        public int Increment { get; set; } = 1;

        [DefaultValue(DBDataType.Int)]
        public DBDataType DBDataType { get; set; } = DBDataType.Int;

        [DefaultValue(0)]
        public int Size { get; set; }

        [DefaultValue(0)]
        public int Scale { get; set; }

        public string NextQuery
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

        /// <summary>
        /// NextInternal no query excution, use if fast increment required, but require restart database sequence(by Save())
        /// </summary>
        /// <returns></returns>
        public long NextInternal()
        {
            Interlocked.CompareExchange(ref changed, 1, 0);
            return Interlocked.Add(ref current, Increment);
        }

        public long Next()
        {
            using (var transaction = new DBTransaction(Schema.Connection))
            {
                try
                {
                    var value = Next(transaction);
                    transaction.Commit();
                    return value;
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    transaction.Rollback();
                    throw ex;
                }
            }
        }

        public long Next(DBTransaction transaction)
        {
            long result = 0;
            result = ParseCurrent(transaction.ExecuteQuery(transaction.AddCommand(NextQuery)));
            Interlocked.CompareExchange(ref current, result, current);
            Interlocked.CompareExchange(ref changed, 0, 1);
            return result;
        }

        private void Save()
        {
            using (var transaction = new DBTransaction(Schema.Connection))
            {
                try
                {
                    Save(transaction);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    Helper.OnException(ex);
                    transaction.Rollback();
                }
            }
        }

        public void Save(DBTransaction transaction)
        {
            if (changed == 0)
                return;
            Interlocked.CompareExchange(ref changed, 0, 1);
            transaction.ExecuteQuery(transaction.AddCommand(FormatSql(DDLType.Alter)));
        }

        private static long ParseCurrent(object result)
        {
            return result is long longvalue ? longvalue :
                result is int intValue ? (long)intValue :
                result is short shortValue ? (short)shortValue :
                result is decimal decimalValue ? (long)decimalValue :
                result is double doubleValue ? (long)doubleValue :
                result is float floatValue ? (long)floatValue :
                long.Parse(result.ToString());
        }

        public long SetCurrent(object result)
        {
            long temp = ParseCurrent(result);
            SetCurrent(temp);
            return temp;
        }

        public void SetCurrent(long temp)
        {
            if (current < temp)
            {
                Current = temp;
            }
        }
    }
}