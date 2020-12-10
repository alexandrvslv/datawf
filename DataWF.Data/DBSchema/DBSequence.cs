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

[assembly: Invoker(typeof(DBSequence), nameof(DBSequence.Current), typeof(DBSequence.CurrentInvoker))]
[assembly: Invoker(typeof(DBSequence), nameof(DBSequence.Increment), typeof(DBSequence.IncrementInvoker))]
[assembly: Invoker(typeof(DBSequence), nameof(DBSequence.Size), typeof(DBSequence.SizeInvoker))]
[assembly: Invoker(typeof(DBSequence), nameof(DBSequence.Scale), typeof(DBSequence.ScaleInvoker))]
[assembly: Invoker(typeof(DBSequence), nameof(DBSequence.DBDataType), typeof(DBSequence.DBDataTypeInvoker))]
[assembly: Invoker(typeof(DBSequence), nameof(DBSequence.NextQuery), typeof(DBSequence.NextQueryInvoker))]
namespace DataWF.Data
{
    public class DBSequence : DBSchemaItem
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

        [JsonIgnore, XmlIgnore]
        public string NextQuery => cacheNextQuery = cacheNextQuery ?? Schema.Connection.System.SequenceNextValue(this);

        [JsonIgnore, XmlIgnore]
        public string CurrentQuery => cacheCurrentQuery = cacheCurrentQuery ?? Schema.Connection.System.SequenceCurrentValue(this);

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
            using (var transaction = new DBTransaction(Schema.Connection))
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
            long result = 0;
            result = Convert(transaction.ExecuteQuery(transaction.AddCommand(NextQuery)));
            Interlocked.CompareExchange(ref current, result, current);
            Interlocked.CompareExchange(ref changed, 0, 1);
            return result;
        }

        public async Task<long> GetNextAsync(DBTransaction transaction)
        {
            long result = 0;
            result = Convert(await transaction.ExecuteQueryAsync(NextQuery, DBExecuteType.Scalar));
            Interlocked.CompareExchange(ref current, result, current);
            Interlocked.CompareExchange(ref changed, 0, 1);
            return result;
        }

        public long GetCurrent()
        {
            using (var transaction = new DBTransaction(Schema.Connection))
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

        public long SetCurrent(object result)
        {
            long temp = Convert(result);
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

        public class CurrentInvoker : Invoker<DBSequence, long>
        {
            public override string Name => nameof(DBSequence.Current);

            public override bool CanWrite => true;

            public override long GetValue(DBSequence target) => target.Current;

            public override void SetValue(DBSequence target, long value) => target.Current = value;
        }

        public class IncrementInvoker : Invoker<DBSequence, int>
        {
            public override string Name => nameof(DBSequence.Increment);

            public override bool CanWrite => true;

            public override int GetValue(DBSequence target) => target.Increment;

            public override void SetValue(DBSequence target, int value) => target.Increment = value;
        }

        public class SizeInvoker : Invoker<DBSequence, int>
        {
            public override string Name => nameof(DBSequence.Size);

            public override bool CanWrite => true;

            public override int GetValue(DBSequence target) => target.Size;

            public override void SetValue(DBSequence target, int value) => target.Size = value;
        }

        public class ScaleInvoker : Invoker<DBSequence, int>
        {
            public override string Name => nameof(DBSequence.Scale);

            public override bool CanWrite => true;

            public override int GetValue(DBSequence target) => target.Scale;

            public override void SetValue(DBSequence target, int value) => target.Scale = value;
        }

        public class DBDataTypeInvoker : Invoker<DBSequence, DBDataType>
        {
            public override string Name => nameof(DBSequence.DBDataType);

            public override bool CanWrite => true;

            public override DBDataType GetValue(DBSequence target) => target.DBDataType;

            public override void SetValue(DBSequence target, DBDataType value) => target.DBDataType = value;
        }

        public class NextQueryInvoker : Invoker<DBSequence, string>
        {
            public override string Name => nameof(DBSequence.NextQuery);

            public override bool CanWrite => false;

            public override string GetValue(DBSequence target) => target.NextQuery;

            public override void SetValue(DBSequence target, string value) { }
        }
    }
}
