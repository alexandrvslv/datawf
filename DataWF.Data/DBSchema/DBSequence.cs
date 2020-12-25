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
using DataWF.Data;
using System;
using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator(Instance = true)]
    public partial class DBSequence : DBSchemaItem
    {
        public static long Convert(object result)
        {
            return result == null || result == DBNull.Value ? 0 : System.Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
        }

        private string cacheNextQuery;
        private long current = 1;
        private int changed = 0;
        private string cacheCurrentQuery;

        public DBSequence()
        { }

        public DBSequence(string name) : base(name)
        { }

        public long Current
        {
            get => current;
            set
            {
                Interlocked.Exchange(ref current, value);
            }
        }

        public int Increment { get; set; } = 1;

        public long StartWith { get; set; } = 1;

        public int Range { get; set; } = -1;

        [DefaultValue(DBDataType.Int)]
        public DBDataType DBDataType { get; set; } = DBDataType.Int;

        [DefaultValue(0)]
        public int Size { get; set; }

        [DefaultValue(0)]
        public int Scale { get; set; }

        [JsonIgnore, XmlIgnore]
        public string NextQuery => cacheNextQuery ??= Schema.Connection.System.SequenceNextValue(this);

        [JsonIgnore, XmlIgnore]
        public string CurrentQuery => cacheCurrentQuery ??= Schema.Connection.System.SequenceCurrentValue(this);

        public override object Clone()
        {
            return new DBSequence()
            {
                Name = name,
                Increment = Increment,
                DBDataType = DBDataType,
                StartWith = StartWith,
                Size = Size,
                Scale = Scale
            };
        }

        public override string FormatSql(DDLType ddlType, bool dependency = false)
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

        public long GetNext()
        {
            using (var transaction = new DBTransaction(Schema))
            {
                try
                {
                    var value = GetNext(transaction);
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

        public long GetNext(DBTransaction transaction)
        {
            return SetNext(Convert(transaction.ExecuteQuery(NextQuery)));
        }

        public async Task<long> GetNextAsync(DBTransaction transaction)
        {
            return SetNext(Convert(await transaction.ExecuteQueryAsync(NextQuery, DBExecuteType.Scalar)));
        }

        private long SetNext(long result)
        {
            Interlocked.CompareExchange(ref changed, 0, 1);
            Interlocked.Exchange(ref current, result);
            return result;
        }

        public long GetCurrent()
        {
            using (var transaction = new DBTransaction(Schema))
            {
                try
                {
                    var value = GetCurrent(transaction);
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

        public long GetCurrent(DBTransaction transaction)
        {
            return Convert(transaction.ExecuteQuery(transaction.AddCommand(CurrentQuery)));
        }

        private void Save()
        {
            using (var transaction = new DBTransaction(Schema))
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
            if (Interlocked.CompareExchange(ref changed, 0, 1) == 0)
                return;
            transaction.ExecuteQuery(transaction.AddCommand(FormatSql(DDLType.Alter)));
        }

        public bool TrySetCurrent(object result)
        {
            long temp = Convert(result);
            return TrySetCurrent(temp);
        }

        public bool TrySetCurrent(long temp)
        {
            if (current < temp)
            {
                Current = temp;
                return true;
            }
            return false;
        }
    }
}
