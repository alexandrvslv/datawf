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
using System.Collections.Generic;
using System.Data;

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
        private IDbConnection connection;
        private DBConnection dbConnection;
        private IDbTransaction transaction;
        private List<DBItem> rows = new List<DBItem>();
        private bool reference = true;
        private bool cancel;
        private object tag;
        private DBTransaction subTransaction;
        private IDBTableView view;

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
            this.dbConnection = config;
            this.connection = connection;
            if (!noTransaction)
                transaction = connection.BeginTransaction(config.IsolationLevel);
            if (!string.IsNullOrEmpty(text))
                AddCommand(text);
        }

        public bool Canceled
        {
            get { return cancel; }
        }

        public object Tag
        {
            get { return tag; }
            set { tag = value; }
        }

        public bool Reference
        {
            get { return reference; }
            set { reference = value; }
        }

        public IDbCommand Command
        {
            get { return command; }
            set { AddCommand(value); }
        }

        public string CommandText
        {
            get { return command != null ? command.CommandText : null; }
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
            if (Commited != null)
                Commited(this, new DBTransactionEventArg(rows));
            foreach (var row in rows)
                row.Accept();

            if (subTransaction != null)
                subTransaction.Commit();
        }

        public void Rollback()
        {
            if (transaction != null && !cancel)
                try
                {
                    transaction.Rollback();
                    cancel = true;
                }
                catch (Exception te) { Helper.OnException(te); }
            foreach (var row in rows)
                row.Reject();
            rows.Clear();

            if (subTransaction != null)
                subTransaction.Rollback();
        }

        public IDbConnection Connection
        {
            get { return connection; }
        }

        public IsolationLevel IsolationLevel
        {
            get { return transaction == null ? dbConnection.IsolationLevel : transaction.IsolationLevel; }
        }

        public void Dispose()
        {
            if (connection != null)
            {
                try
                {
                    foreach (var item in commands)
                    {
                        item.Cancel();
                        foreach (IDataParameter param in item.Parameters)
                            if (param.Value is IDisposable)
                                ((IDisposable)param.Value).Dispose();
                        item.Dispose();
                    }
                    commands.Clear();

                    if (transaction != null)
                        transaction.Dispose();
                    if (subTransaction != null)
                        subTransaction.Dispose();
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();

                    transaction = null;
                    connection = null;
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
            dparam.Value = value;
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
                command.Connection = connection;
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
                command = DBService.CreateCommand(connection, query, transaction);
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

        public void BeginSubTransaction()
        {
            subTransaction = dbConnection.System == DBSystem.SQLite ? new DBTransaction(dbConnection, connection) : new DBTransaction(dbConnection);
        }

        public void BeginSubTransaction(DBSchema schema)
        {
            subTransaction = new DBTransaction(schema.Connection);
        }

        public void BeginSubTransaction(DBConnection config)
        {
            subTransaction = new DBTransaction(config);
        }

        public DBTransaction SubTransaction
        {
            get { return subTransaction; }
        }

        public IDBTableView View
        {
            get { return view; }
            set { view = value; }
        }

        public void Cancel()
        {
            if (!cancel)
            {
                cancel = true;
                if (subTransaction != null)
                    subTransaction.Cancel();
                Rollback();
            }
        }
    }
}

