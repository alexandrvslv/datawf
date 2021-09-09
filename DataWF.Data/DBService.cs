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
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public delegate object ExecuteDelegate(ExecuteEventArgs arg);

    public delegate void DBExecuteDelegate(DBExecuteEventArg arg);

    public delegate void DBItemEditEventHandler(DBItemEventArgs arg);

    public class ExecuteEventArgs : CancelEventArgs
    {
        public ExecuteEventArgs(IDBSchema schema, DBTable table, string query)
        {
            Schema = schema;
            Table = table;
            Query = query;
        }

        public IDBSchema Schema { get; set; }

        public DBTable Table { get; set; }

        public string Query { get; set; }
    }

    public class DBExecuteEventArg : EventArgs
    {
        public DBExecuteEventArg()
        {
        }

        public object Rezult { get; set; }

        public TimeSpan Time { get; set; }

        public string Query { get; set; }

        public DBExecuteType Type { get; set; }
    }

    /// <summary>
    /// Service for connection
    /// </summary>
    public static class DBService
    {
        //private static object[] fabricparam = new object[1];
        //private static Type[] param = new Type[] { typeof(DBItem) };
        //private static Type[] param2 = new Type[] { typeof(DBRow) };

        private static readonly HashSet<Func<DBTransaction, ValueTask>> transactionCommitHandler = new HashSet<Func<DBTransaction, ValueTask>>();
        private static readonly HashSet<Func<DBItemEventArgs, ValueTask>> itemAcceptHandler = new HashSet<Func<DBItemEventArgs, ValueTask>>();
        private static readonly HashSet<Func<DBItemEventArgs, ValueTask>> itemRejectHandler = new HashSet<Func<DBItemEventArgs, ValueTask>>();
        private static readonly HashSet<Func<DBItemEventArgs, ValueTask>> itemUpdatedHandler = new HashSet<Func<DBItemEventArgs, ValueTask>>();
        private static readonly HashSet<Func<DBItemEventArgs, ValueTask>> itemLoginingHandler = new HashSet<Func<DBItemEventArgs, ValueTask>>();
        private static readonly HashSet<Func<DBItemEventArgs, ValueTask>> itemUpdatingHandler = new HashSet<Func<DBItemEventArgs, ValueTask>>();

        public static void AddTransactionCommit(Func<DBTransaction, ValueTask> function)
        {
            transactionCommitHandler.Add(function);
        }

        public static void RemoveTransactionCommit(Func<DBTransaction, ValueTask> function)
        {
            transactionCommitHandler.Remove(function);
        }

        internal static void OnTransactionCommit(DBTransaction transaction)
        {
            foreach (var function in transactionCommitHandler)
            {
                _ = function(transaction);
            }
        }

        public static void AddItemUpdating(Func<DBItemEventArgs, ValueTask> function)
        {
            itemUpdatingHandler.Add(function);
        }

        public static void RemoveItemUpdating(Func<DBItemEventArgs, ValueTask> function)
        {
            itemUpdatingHandler.Remove(function);
        }

        internal static void OnUpdating(DBItemEventArgs e)
        {
            foreach (var function in itemUpdatingHandler)
            {
                _ = function(e);
            }
        }

        public static void AddItemLoging(Func<DBItemEventArgs, ValueTask> function)
        {
            itemLoginingHandler.Add(function);
        }

        public static void RemoveItemLoging(Func<DBItemEventArgs, ValueTask> function)
        {
            itemLoginingHandler.Remove(function);
        }

        internal static void OnLogItem(DBItemEventArgs e)
        {
            foreach (var function in itemLoginingHandler)
            {
                _ = function(e);
            }
        }

        public static void AddItemUpdated(Func<DBItemEventArgs, ValueTask> function)
        {
            itemUpdatedHandler.Add(function);
        }

        public static void RemoveItemUpdated(Func<DBItemEventArgs, ValueTask> function)
        {
            itemUpdatedHandler.Remove(function);
        }

        internal static void OnUpdated(DBItemEventArgs e)
        {
            foreach (var function in itemUpdatedHandler)
            {
                _ = function(e);
            }
        }

        public static void AddRowAccept(Func<DBItemEventArgs, ValueTask> function)
        {
            itemAcceptHandler.Add(function);
        }

        public static void RemoveRowAccept(Func<DBItemEventArgs, ValueTask> function)
        {
            itemAcceptHandler.Remove(function);
        }

        internal static void OnAccept(DBItemEventArgs e)
        {
            foreach (var function in itemAcceptHandler)
            {
                _ = function(e);
            }
        }

        public static void AddRowReject(Func<DBItemEventArgs, ValueTask> function)
        {
            itemRejectHandler.Add(function);
        }

        public static void RemoveRowReject(Func<DBItemEventArgs, ValueTask> function)
        {
            itemRejectHandler.Remove(function);
        }

        internal static void OnReject(DBItemEventArgs e)
        {
            foreach (var function in itemRejectHandler)
            {
                _ = function(e);
            }
        }

        public static int GetIntValue(object value)
        {
            if (value == null)
                return 0;
            if (value is int)
                return (int)value;
            if (value is int?)
                return ((int?)value).Value;
            return int.TryParse(value.ToString(), out int result) ? result : 0;
        }

         

        public static bool Equal<T>(T x, T y)
        {
            if (x is string xString && y is string yString)
            {
                return string.Equals(xString, yString, StringComparison.Ordinal);
            }
            if (x is byte[] xByte && y is byte[] yByte)
            {
                return ByteArrayComparer.Default.Equals(xByte, yByte);
            }
            return EqualityComparer<T>.Default.Equals(x, y);
        }

        public static bool Equal(object x, object y)
        {
            if (x == null)
            {
                return y == null;
            }
            if (y == null)
            {
                return false;
            }

            var equal = false;
            if (x.GetType() == typeof(string))
                equal = string.Equals(x.ToString(), y.ToString(), StringComparison.Ordinal);
            else if (x is Enum || y is Enum)
                equal = ((int)x).Equals((int)y);
            else if (x is byte[] byteX && y is byte[] byteY)
                equal = Helper.EqualsBytes(byteX, byteY);
            else
                equal = x.Equals(y);
            return equal;
        }

        public static object GetItem(List<KeyValuePair<string, object>> list, string key)
        {
            for (int i = 0; i < list.Count; i++)
                if (string.Compare(list[i].Key, key, true) == 0)
                    return list[i].Value;
            return DBNull.Value;
        }       

    }
}
