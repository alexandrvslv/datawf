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
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public class DBTransaction : IDbTransaction
    {
        private readonly Dictionary<string, IDbCommand> commands = new Dictionary<string, IDbCommand>(StringComparer.Ordinal);
        private readonly HashSet<DBItem> items = new HashSet<DBItem>();
        private Dictionary<DBConnection, DBTransaction> subTransactions;
        private IDbCommand command;
        private IDbTransaction transaction;

        public DBTransaction(IDBSchemaItem schemaItem, IUserIdentity caller = null, bool noTransaction = false)
            : this(schemaItem.Schema, caller, noTransaction)
        {
        }

        public DBTransaction(IDBSchema schema, IUserIdentity caller = null, bool noTransaction = false)
            : this(schema.Connection, caller, noTransaction)
        {
            Schema = schema;
        }

        public DBTransaction(DBConnection config, IUserIdentity caller = null, bool noTransaction = false)
            : this(config, caller, config.GetConnection(), noTransaction)
        { }

        public DBTransaction(DBConnection config, IUserIdentity caller, IDbConnection connection, bool noTransaction = false)
        {
            DbConnection = config;
            Caller = caller;
            Connection = connection;
            if (!noTransaction)
            {
                transaction = connection.BeginTransaction(config.IsolationLevel);
            }
            //Debug.WriteLine($"New DBTransaction owner:{owner} connection:{config} is NON{noTransaction}");
        }
        public IDBSchema Schema { get; set; }

        public IUserIdentity Caller { get; private set; }

        public DBTransactionState State { get; private set; }

        public IDbCommand Command
        {
            get => command;
            set => AddCommand(value);
        }

        public string CommandText
        {
            get => command?.CommandText;
            set => AddCommand(value);
        }

        public HashSet<DBItem> Items => items;

        public IsolationLevel IsolationLevel => transaction == null ? DbConnection.IsolationLevel : transaction.IsolationLevel;

        public IEnumerable<DBTransaction> SubTransactions => subTransactions.Values;

        public DBConnection DbConnection { get; private set; }

        public IDbConnection Connection { get; private set; }

        public DbDataReader Reader { get; set; }

        public DBLoadParam ReaderParam { get; set; }

        public DBUpdateState ReaderState { get; set; }

        public List<DBReaderFields> ReaderFields { get; set; }
        
        public IDBTableView View { get; set; }

        public DBItem UserLog { get; set; }

        public HashSet<DBColumn> ReferencingStack { get; set; } = new HashSet<DBColumn>();

        public int ReferencingRecursion { get; set; }

        public bool NoLogs { get; set; }

        public bool Replication { get; set; }

        public void Commit()
        {
            CloseReader();
            if (transaction != null && State != DBTransactionState.Commit)
            {
                try
                {
                    transaction.Commit();
                }
                catch (Exception te)
                {
                    foreach (var row in items)
                    {
                        row.Reject(Caller);
                    }

                    Helper.OnException(te);
                    return;
                }
            }
            State = DBTransactionState.Commit;
            foreach (var row in items)
            {
                row.Accept(Caller);
            }

            if (subTransactions != null)
            {
                foreach (var transaction in subTransactions.Values)
                {
                    transaction.Commit();
                }
            }

            DBService.OnTransactionCommit(this);
        }

        private void CloseReader()
        {
            if (!(Reader?.IsClosed ?? true))
            {
                Reader.Close();
            }
        }

        public void Rollback()
        {
            CloseReader();
            if (transaction != null && State != DBTransactionState.Rollback)
            {
                try
                {
                    transaction.Rollback();
                }
                catch (Exception te)
                {
                    Helper.OnException(te);
                }
            }
            State = DBTransactionState.Rollback;
            foreach (var row in items)
            {
                row.Reject(Caller);
            }
            items.Clear();


            if (subTransactions != null)
            {
                foreach (var transaction in subTransactions.Values)
                {
                    transaction.Rollback();
                }
            }
            DBService.OnTransactionCommit(this);
        }

        public void Dispose()
        {
            if (Connection == null)
            {
                return;
            }
            try
            {
                foreach (var item in commands.Values)
                {
                    //TODO CHECK item.Cancel();
                    //try
                    //{
                    //    foreach (IDataParameter param in item.Parameters)
                    //    {
                    //        if (param.Value is IDisposable dispValue)
                    //            dispValue.Dispose();
                    //    }
                    //}
                    //catch (Exception ex) { Helper.OnException(ex); }
                    item?.Dispose();
                }
                commands.Clear();

                Reader?.Dispose();
                transaction?.Dispose();

                if (subTransactions != null)
                {
                    foreach (var subTransaction in subTransactions.Values.ToList())
                    {
                        subTransaction.Dispose();
                    }
                }
            }
            finally
            {
                if (Connection.State != ConnectionState.Closed)
                {
                    Connection.Close();
                }
                transaction = null;
                Connection = null;
                command = null;
                ReaderFields = null;
                items.Clear();
            }
            //Debug.WriteLine($"Dispose DBTransaction owner:{Owner} connection:{DbConnection}");
        }

        public IDbCommand AddCommand(IDbCommand newCommand)
        {
            //if (cancel)
            //    throw new Exception("Transaction is Canceled!");
            if (newCommand != command)
            {
                if (commands.TryGetValue(newCommand.CommandText, out var existCommand)
                    && existCommand != newCommand)
                {
                    existCommand.Dispose();
                }
                commands[newCommand.CommandText] = newCommand;

                command = newCommand;
                command.Connection = Connection;

                if (transaction != null)
                {
                    command.Transaction = transaction;
                }
            }
            return command;
        }

        public IDbCommand AddCommand(string query, CommandType commandType = CommandType.Text)
        {
            if (string.IsNullOrEmpty(query))
                return null;

            //if (cancel)
            //    throw new Exception("Transaction is Canceled!");
            command = null;
            if (commands.TryGetValue(query, out var exist))
            {
                command = exist;
            }
            else
            {
                commands[query] = command = CreateCommand(Connection, query, transaction);
            }
            command.CommandType = commandType;
            return command;
        }

        public IDbCommand CreateCommand(IDbConnection connection, string text = null, IDbTransaction transaction = null)
        {
            IDbCommand command = connection.CreateCommand();
            command.CommandTimeout = connection.ConnectionTimeout;
            command.CommandText = text;
            if (transaction != null)
                command.Transaction = transaction;

            return command;
        }

        public void RemoveCommand(IDbCommand rcommand)
        {
            //if (cancel)
            //    throw new Exception("Transaction is Canceled!");
            commands.Remove(rcommand.CommandText);
            if (command == rcommand)
                command = commands.Values.FirstOrDefault();
        }

        public DBTransaction GetSubTransaction()
        {
            return GetSubTransaction(DbConnection, false);
        }

        public DBTransaction GetSubTransaction(DBSchema schema, bool checkSelf = true)
        {
            return GetSubTransaction(schema.Connection, checkSelf);
        }

        public DBTransaction GetSubTransaction(DBConnection config, bool checkSelf = true, bool noTransaction = false)
        {
            if (checkSelf && config == DbConnection && Reader == null)
                return this;
            if (subTransactions == null)
            {
                subTransactions = new Dictionary<DBConnection, DBTransaction>();
            }
            //TODO Check several subtransaction with same config (IDBConnections leak!!!)
            else if (subTransactions.TryGetValue(config, out var subTransaction) && subTransaction.Reader == null)
            {
                return subTransaction;
            }
            return subTransactions[config] = new DBTransaction(config, Caller, noTransaction);
            //TODO Check several opened connections in sqlite config.System == DBSystem.SQLite && DbConnection.System == DBSystem.SQLite
        }

        public void RemoveSubtransaction(DBTransaction subTransaction)
        {
            if (subTransactions != null)
            {
                foreach (var entry in subTransactions)
                {
                    if (entry.Value == subTransaction)
                    {
                        subTransactions.Remove(entry.Key);
                        return;
                    }
                }
            }
        }

        public void Cancel()
        {
            if (State == DBTransactionState.Default)
            {
                Rollback();
            }
        }

        public QResult ExecuteQResult()
        {
            var list = new QResult();
            ExecuteQResult(Command, list);
            return list;
        }

        public QResult ExecuteQResult(string commandText)
        {
            return ExecuteQResult(AddCommand(commandText));
        }

        public QResult ExecuteQResult(IDbCommand command)
        {
            var list = new QResult();
            ExecuteQResult(AddCommand(command), list);
            return list;
        }

        public void ExecuteQResult(IDbCommand command, QResult list)
        {
            list.Values.Clear();
            list.Columns.Clear();
            using (var reader = ExecuteQuery(command, DBExecuteType.Reader) as IDataReader)
            {
                int fCount = reader.FieldCount;
                for (int i = 0; i < fCount; i++)
                {
                    var name = reader.GetName(i);
                    list.Columns.Add(name, new QField { Index = i, Name = name, DataType = reader.GetFieldType(i) });
                }
                list.OnColumnsLoaded();
                while (reader.Read())
                {
                    var objects = new object[fCount];
                    reader.GetValues(objects);
                    list.Values.Add(objects);
                }
                reader.Close();
                list.OnLoaded();
            }
        }

        public List<Dictionary<string, object>> ExecuteListDictionary(IDbCommand command)
        {
            var list = new List<Dictionary<string, object>>();
            using (var reader = ExecuteQuery(command, DBExecuteType.Reader) as IDataReader)
            {
                int fCount = reader.FieldCount;
                while (reader.Read())
                {
                    var objects = new Dictionary<string, object>(fCount, StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < fCount; i++)
                        objects[reader.GetName(i)] = reader.GetValue(i);
                    list.Add(objects);
                }
                reader.Close();
            }
            return list;
        }

        public object ExecuteQuery(string commandText, DBExecuteType type = DBExecuteType.Scalar, CommandBehavior behavior = CommandBehavior.Default)
        {
            return ExecuteQuery(AddCommand(commandText), type, behavior);
        }

        public object ExecuteQuery(IDbCommand command, DBExecuteType type = DBExecuteType.Scalar, CommandBehavior behavior = CommandBehavior.Default)
        {
            object buf = null;
#if DEBUG
            var watch = new Stopwatch();
#endif
            try
            {
#if DEBUG
                watch.Start();
#endif
                switch (type)
                {
                    case DBExecuteType.Scalar:
                        buf = command.ExecuteScalar();
                        break;
                    case DBExecuteType.Reader:
                        buf = command.ExecuteReader(behavior);
                        break;
                    case DBExecuteType.NoReader:
                        buf = command.ExecuteNonQuery();
                        break;
                }
#if DEBUG
                watch.Stop();
                OnExecute(type, command.CommandText, watch.Elapsed, buf);
#else
                OnExecute(type, command.CommandText, TimeSpan.Zero, buf);
#endif
            }
            catch (Exception ex)
            {
                Rollback();
#if DEBUG
                OnExecute(type, command.CommandText, watch.Elapsed, ex);
#else
                OnExecute(type, command.CommandText, TimeSpan.Zero, ex);
#endif
                throw;
            }
            return buf;
        }

        public Task<object> ExecuteQueryAsync(string commandText, DBExecuteType type = DBExecuteType.Scalar, CommandBehavior behavior = CommandBehavior.Default)
        {
            return ExecuteQueryAsync(AddCommand(commandText), type, behavior);
        }

        public async Task<object> ExecuteQueryAsync(IDbCommand command, DBExecuteType type = DBExecuteType.Scalar, CommandBehavior behavior = CommandBehavior.Default)
        {
            object buf = null;
#if DEBUG
            var watch = new Stopwatch();
#endif
            try
            {
#if DEBUG
                watch.Start();
#endif
                buf = await DbConnection.System.ExecuteQueryAsync(command, type, behavior);
#if DEBUG
                watch.Stop();
                OnExecute(type, command.CommandText, watch.Elapsed, buf);
#else
                OnExecute(type, command.CommandText, TimeSpan.Zero, buf);
#endif
            }
            catch (Exception ex)
            {
                Rollback();
#if DEBUG
                OnExecute(type, command.CommandText, watch.Elapsed, ex);
#else
                OnExecute(type, command.CommandText, TimeSpan.Zero, ex);
#endif
                throw;
            }
            return buf;
        }

        public event DBExecuteDelegate Execute;

        internal void OnExecute(DBExecuteType type, string text, TimeSpan ms, object rez)
        {
            if (rez is Exception ex)
            {
                Helper.Log(nameof(DBTransaction), ex.Message, text, StatusType.Warning);
            }

            Execute?.Invoke(new DBExecuteEventArg { Time = ms, Query = text, Type = type, Rezult = rez });
        }

        public bool AddItem(DBItem item, bool addReferening = false)
        {
            if (!Items.Contains(item))
            {
                Items.Add(item);
                if (addReferening)
                {
                    foreach (DBItem subItem in item.GetPropertyReferencing())
                    {
                        if (subItem.IsReferencingChanged)
                        {
                            AddItem(subItem);
                        }
                    }
                }
                return true;
            }
            return item.IsReferencingChanged;
        }

        public bool RemoveItem(DBItem item)
        {
            return Items.Remove(item);
        }

        public uint ReadUInt(int index)
        {
            return DbConnection.System.GetUInt(Reader, index);
        }

        public TimeSpan GetTimeSpan(int index)
        {
            return DbConnection.System.GetTimeSpan(Reader, index);
        }

        public void PrepareStatements(IDbCommand command)
        {
            DbConnection.System.PrepareStatements(command);
        }
    }

    public enum DBTransactionState
    {
        Default,
        Commit,
        Rollback
    }
}

