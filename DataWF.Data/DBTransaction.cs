﻿using DataWF.Common;
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
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

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
        private List<IDbCommand> commands = new List<IDbCommand>();
        private IDbCommand command;
        private IDbTransaction transaction;
        private List<DBItem> rows = new List<DBItem>();
        private bool cancel;
        private Dictionary<DBConnection, DBTransaction> subTransactions;

        public DBTransaction()
            : this(DBService.DefaultSchema.Connection)
        {
        }

        public DBTransaction(DBConnection config, string text = "", bool noTransaction = false)
            : this(config, config.GetConnection(), text, noTransaction)
        {
        }

        public DBTransaction(DBConnection config, IDbConnection connection, string text = "", bool noTransaction = false)
        {
            DbConnection = config;
            Connection = connection;
            if (!noTransaction)
                transaction = connection.BeginTransaction(config.IsolationLevel);
            if (!string.IsNullOrEmpty(text))
                AddCommand(text);
        }

        public bool Canceled
        {
            get { return cancel; }
        }

        public object Tag { get; set; }

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
            if (transaction != null)
                try { transaction.Commit(); }
                catch (Exception te)
                {
                    foreach (var row in rows)
                        row.Reject();
                    Helper.OnException(te);
                    return;
                }

            Commited?.Invoke(this, new DBTransactionEventArg(rows));
            foreach (var row in rows)
                row.Accept();

            if (subTransactions != null)
            {
                foreach (var transaction in subTransactions.Values)
                {
                    transaction.Commit();
                }
            }
        }

        public void Rollback()
        {
            if (transaction != null && !cancel)
            {
                try
                {
                    transaction.Rollback();
                    cancel = true;
                }
                catch (Exception te) { Helper.OnException(te); }
            }
            foreach (var row in rows)
            {
                row.Reject();
            }
            rows.Clear();

            if (subTransactions != null)
            {
                foreach (var transaction in subTransactions.Values)
                {
                    transaction.Commit();
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

        public void Dispose()
        {
            if (Connection != null)
            {
                try
                {
                    foreach (var item in commands)
                    {
                        item.Cancel();
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
                        foreach (var subTransaction in subTransactions.Values)
                        {
                            subTransaction.Dispose();
                        }
                    }
                }
                finally
                {
                    if (Connection.State == ConnectionState.Open)
                        Connection.Close();

                    transaction = null;
                    Connection = null;
                    command = null;
                    rows.Clear();
                }
            }

        }

        public IDataParameter AddParameter(IDbCommand ncommand, string name, object value)
        {
            IDataParameter dparam = null;
            foreach (IDataParameter param in ncommand.Parameters)
                if (param.ParameterName == name)
                {
                    dparam = param;
                    break;
                }
            if (dparam == null)
            {
                dparam = ncommand.CreateParameter();
                dparam.ParameterName = name;
                ncommand.Parameters.Add(dparam);
            }
            dparam.Value = value ?? DBNull.Value;
            return dparam;
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
                command = DBService.CreateCommand(Connection, query, transaction);
                commands.Add(command);
            }
            command.CommandType = commandType;
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

        public DBTransaction GetSubTransaction(DBSchema schema, bool check = true)
        {
            return GetSubTransaction(schema.Connection, check);
        }

        public DBTransaction GetSubTransaction(DBConnection config, bool checkSelf = true)
        {
            if (checkSelf && config == DbConnection)
                return this;
            if (subTransactions == null)
            {
                subTransactions = new Dictionary<DBConnection, DBTransaction>();
            }
            else if (subTransactions.TryGetValue(config, out var subTransaction))
            {
                return subTransaction;
            }
            return subTransactions[config] = new DBTransaction(config);
            //TODO Check several opened connections in sqlite config.System == DBSystem.SQLite && DbConnection.System == DBSystem.SQLite
        }

        public void Cancel()
        {
            if (!cancel)
            {
                cancel = true;
                if (subTransactions != null)
                {
                    foreach (var subTransaction in subTransactions.Values)
                        subTransaction.Cancel();
                }
                Rollback();
            }
        }

        public QResult ExecuteQResult()
        {
            var list = new QResult();
            ExecuteQResult(list);
            return list;
        }

        public void ExecuteQResult(QResult list)
        {
            list.Values.Clear();
            list.Columns.Clear();
            using (var reader = ExecuteQuery(DBExecuteType.Reader) as IDataReader)
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

        public object ExecuteQuery(IDbCommand command, DBExecuteType type = DBExecuteType.Scalar)
        {
            object buf = null;
            var watch = new Stopwatch();
            try
            {
                Debug.WriteLine(command.CommandText);
                watch.Start();
                switch (type)
                {
                    case DBExecuteType.Scalar:
                        buf = command.ExecuteScalar();
                        break;
                    case DBExecuteType.Reader:
                        buf = command.ExecuteReader();
                        break;
                    case DBExecuteType.NoReader:
                        buf = command.ExecuteNonQuery();
                        break;
                }

                watch.Stop();
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                ex.HelpLink = Environment.StackTrace;
                buf = ex;
            }
            finally
            {
                DBService.OnExecute(type, command.CommandText, watch.Elapsed, buf);
                if (buf is Exception)
                    throw (Exception)buf;
            }
            return buf;
        }
    }
}

