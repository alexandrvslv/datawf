using DataWF.Common;
//
//  DBTransaction.cs
//
//  Author:
//       Alexandr <alexandr_vslv@mail.ru>
//
//  Copyright (c) 2015 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace DataWF.Data
{
    public class DBTransactionEventArg : EventArgs
    {
        private List<DBItem> rows;

        public DBTransactionEventArg(List<DBItem> rows)
        {
            this.rows = rows;
        }

        public List<DBItem> Rows
        {
            get { return rows; }
        }
    }

    public class DBTransaction : IDbTransaction
    {
        public static EventHandler<DBTransactionEventArg> Commited;

        [ThreadStatic]
        private static DBTransaction current;
        private static ConcurrentQueue<DBTransaction> queue = new ConcurrentQueue<DBTransaction>();
        public static DBTransaction Current
        {
            get => current;
            internal set
            {
                if (current != null && value != null)
                {
                    queue.Enqueue(current);
                    //current.subTransactions[value.DbConnection] = value;
                }
                else
                {
                    current = value;
                }
            }
        }

        public static DBTransaction GetTransaction(object owner, DBConnection connection, bool noTransaction = false, DBLoadParam param = DBLoadParam.None, IDBTableView synch = null)
        {
            var transaction = Current?.GetSubTransaction(connection, true, noTransaction) ?? new DBTransaction(owner, connection, noTransaction);
            if (transaction.View == null)
                transaction.View = synch;
            if (transaction.ReaderParam == DBLoadParam.None)
                transaction.ReaderParam = param;
            return transaction;
        }

        private List<IDbCommand> commands = new List<IDbCommand>();
        private IDbCommand command;
        private IDbTransaction transaction;
        private List<DBItem> rows = new List<DBItem>();
        private Dictionary<DBConnection, DBTransaction> subTransactions;

        public DBTransaction()
            : this(DBService.Schems.DefaultSchema.Connection)
        { }

        public DBTransaction(DBConnection config)
            : this(config, config)
        { }

        public DBTransaction(object owner, DBConnection config, bool noTransaction = false)
            : this(owner, config, config.GetConnection(), noTransaction)
        { }

        public DBTransaction(object owner, DBConnection config, IDbConnection connection, bool noTransaction = false)
        {
            Owner = owner;
            Current = this;
            DbConnection = config;
            Connection = connection;
            if (!noTransaction)
                transaction = connection.BeginTransaction(config.IsolationLevel);
            //Debug.WriteLine($"New DBTransaction owner:{owner} connection:{config} is NON{noTransaction}");
        }

        public object Owner { get; private set; }

        public bool Canceled { get; private set; }

        public bool Reference { get; set; } = true;

        public IDbCommand Command
        {
            get { return command; }
            set { AddCommand(value); }
        }

        public string CommandText
        {
            get { return command?.CommandText; }
            set { AddCommand(value); }
        }

        public List<DBItem> Rows
        {
            get { return rows; }
        }

        public void Commit()
        {
            Commit(Owner as IUserIdentity);
        }

        public void Commit(IUserIdentity user)
        {
            if (transaction != null)
                try { transaction.Commit(); }
                catch (Exception te)
                {
                    foreach (var row in rows)
                        row.Reject(user);
                    Helper.OnException(te);
                    return;
                }

            Commited?.Invoke(this, new DBTransactionEventArg(rows));
            foreach (var row in rows)
                row.Accept(user);

            if (subTransactions != null)
            {
                foreach (var transaction in subTransactions.Values)
                {
                    transaction.Commit(user);
                }
            }
        }

        public void Rollback()
        {
            Rollback(Owner as IUserIdentity);
        }

        public void Rollback(IUserIdentity user)
        {
            if (transaction != null && !Canceled)
            {
                try
                {
                    transaction.Rollback();
                    Canceled = true;
                }
                catch (Exception te) { Helper.OnException(te); }
            }
            foreach (var row in rows)
            {
                row.Reject(user);
            }
            rows.Clear();

            if (subTransactions != null)
            {
                foreach (var transaction in subTransactions.Values)
                {
                    transaction.Rollback(user);
                }
            }
        }

        public IsolationLevel IsolationLevel
        {
            get { return transaction == null ? DbConnection.IsolationLevel : transaction.IsolationLevel; }
        }

        public IEnumerable<DBTransaction> SubTransactions
        {
            get { return subTransactions.Values; }
        }

        public DBConnection DbConnection { get; private set; }

        public IDbConnection Connection { get; private set; }

        public IDataReader Reader { get; set; }

        public List<DBColumn> ReaderColumns { get; set; }

        public DBLoadParam ReaderParam { get; set; }

        public DBUpdateState ReaderState { get; set; }

        public int ReaderPrimaryKey { get; internal set; } = -1;

        public int ReaderStampKey { get; internal set; } = -1;

        public int ReaderItemTypeKey { get; internal set; } = -1;

        public IDBTableView View { get; set; }

        public DBItem UserLog { get; set; }

        public void Dispose()
        {
            if (Connection != null)
            {
                try
                {
                    foreach (var item in commands)
                    {
                        //TODO CHECK item.Cancel();
                        foreach (IDataParameter param in item.Parameters)
                        {
                            if (param.Value is IDisposable)
                                ((IDisposable)param.Value).Dispose();
                        }
                        item.Dispose();
                    }
                    commands.Clear();

                    if (transaction != null)
                        transaction.Dispose();
                    if (subTransactions != null)
                    {
                        foreach (var subTransaction in subTransactions.Values.ToArray())
                        {
                            subTransaction.Dispose();
                        }
                    }
                }
                finally
                {
                    if (Current == this)
                    {
                        Current = null;
                    }
                    else if (Current != null)
                    {
                        Current.RemoveSubtransaction(this);
                    }
                    if (Connection.State == ConnectionState.Open)
                    {
                        Connection.Close();
                    }
                    transaction = null;
                    Connection = null;
                    command = null;
                    commands = null;
                    ReaderColumns = null;
                    rows.Clear();
                }

                //Debug.WriteLine($"Dispose DBTransaction owner:{Owner} connection:{DbConnection}");
            }

        }

        public IDbCommand AddCommand(IDbCommand ncommand)
        {
            //if (cancel)
            //    throw new Exception("Transaction is Canceled!");
            if (ncommand != command)
            {
                if (!commands.Contains(ncommand))
                    commands.Add(ncommand);
                command = ncommand;
                command.Connection = Connection;
                if (transaction != null)
                    command.Transaction = transaction;
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
            foreach (var item in commands)
                if (item.CommandText == query)
                {
                    command = item;
                    break;
                }
            if (command == null)
            {
                command = CreateCommand(Connection, query, transaction);
                commands.Add(command);
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
            commands.Remove(rcommand);
            if (command == rcommand)
                command = commands.Count > 0 ? commands[0] : null;
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
            return subTransactions[config] = new DBTransaction(Owner, config, noTransaction);
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
            Cancel(Owner as IUserIdentity);
        }

        public void Cancel(IUserIdentity user)
        {
            if (!Canceled)
            {
                Canceled = true;
                if (subTransactions != null)
                {
                    foreach (var subTransaction in subTransactions.Values)
                        subTransaction.Cancel(user);
                }
                Rollback(user);
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

        public List<Dictionary<string, object>> ExecuteListDictionary()
        {
            var list = new List<Dictionary<string, object>>();
            using (var reader = ExecuteQuery(DBExecuteType.Reader) as IDataReader)
            {
                int fCount = reader.FieldCount;
                while (reader.Read())
                {
                    var objects = new Dictionary<string, object>(fCount, StringComparer.InvariantCultureIgnoreCase);
                    for (int i = 0; i < fCount; i++)
                        objects.Add(reader.GetName(i), reader.GetValue(i));
                    list.Add(objects);
                }
                reader.Close();
            }
            return list;
        }

        public object ExecuteQuery(DBExecuteType type = DBExecuteType.Scalar)
        {
            return ExecuteQuery(Command, type);
        }

        public object ExecuteQuery(string commandText, DBExecuteType type = DBExecuteType.Scalar)
        {
            return ExecuteQuery(AddCommand(CommandText), type);
        }

        public object ExecuteQuery(IDbCommand command, DBExecuteType type = DBExecuteType.Scalar, CommandBehavior behavior = CommandBehavior.Default)
        {
            object buf = null;
            var watch = new Stopwatch();
            try
            {
                //Debug.WriteLine(command.Connection.ConnectionString);
                //Debug.WriteLine(command.CommandText);
                watch.Start();
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

                watch.Stop();
            }
            catch (Exception ex)
            {
                Rollback(null);
                ex.HelpLink = Environment.StackTrace;
                buf = ex;
            }
            finally
            {
                OnExecute(type, command.CommandText, watch.Elapsed, buf);
                if (buf is Exception)
                {
                    throw (Exception)buf;
                }
            }
            return buf;
        }

        public event DBExecuteDelegate Execute;

        internal void OnExecute(DBExecuteType type, string text, TimeSpan ms, object rez)
        {
            Execute?.Invoke(new DBExecuteEventArg { Time = ms, Query = text, Type = type, Rezult = rez });
        }
    }
}

