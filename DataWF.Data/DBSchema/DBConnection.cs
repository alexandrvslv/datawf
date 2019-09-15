﻿using DataWF.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBConnectionList : SelectableList<DBConnection>
    {
        public DBConnectionList()
        {
            Indexes.Add(DBConnection.NameInvoker.Instance);
        }

        public DBConnection this[string name]
        {
            get { return SelectOne(nameof(DBConnection.Name), CompareType.Equal, name); }
        }
    }
    public class DBConnection : INotifyPropertyChanged, IDisposable
    {
        private string name = "";
        private string host = "";
        private uint port = 0;
        private string user = "";
        private byte[] password = { };
        private string extend;
        private string database = "";
        private string schem = "";
        private int timeout = 80;
        private bool? pool;
        private bool encrypt;
        private bool integrSec;
        private IsolationLevel level = IsolationLevel.ReadUncommitted;
        private object _locker = new object();
        private string systemName;
        private DBSystem system;
        internal HashSet<IDbConnection> Buffer = new HashSet<IDbConnection>();
        private string path;

        public DBConnection()
        { }

        public DBConnection(string name)
        {
            Name = name;
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [Browsable(false)]
        public string SystemName
        {
            get { return systemName; }
            set
            {
                systemName = value;
                system = GetSystem();
                OnPropertyChanged(nameof(System));
            }
        }

        [XmlIgnore, JsonIgnore, Category("1. Host")]
        public DBSystem System
        {
            get { return system ?? (system = GetSystem()); }
            set
            {
                system = value;
                systemName = value?.Name;
            }
        }

        private DBSystem GetSystem()
        {
            switch (SystemName)
            {
                case nameof(DBSystem.MSSql): return DBSystem.MSSql;
                case nameof(DBSystem.MySql): return DBSystem.MySql;
                case nameof(DBSystem.Oracle): return DBSystem.Oracle;
                case nameof(DBSystem.Postgres): return DBSystem.Postgres;
                default: return DBSystem.SQLite;
            }
        }

        [Category("1. Host")]
        public string Host
        {
            get { return host; }
            set
            {
                if (host == value)
                    return;
                host = value;
                OnPropertyChanged(nameof(Host));
            }
        }

        [Category("1. Host")]
        public uint Port
        {
            get { return port; }
            set
            {
                if (port == value)
                    return;
                port = value;
                OnPropertyChanged(nameof(Port));
            }
        }

        [Category("2. Database")]
        public string DataBase
        {
            get { return database; }
            set
            {
                if (database == value)
                    return;
                database = value;
                OnPropertyChanged(nameof(DataBase));
            }
        }

        [Category("2. Database")]
        public string Schema
        {
            get { return schem; }
            set
            {
                if (schem == value)
                    return;
                schem = value;
                OnPropertyChanged(nameof(Schema));
            }
        }

        [Category("2. Database")]
        public string User
        {
            get { return user; }
            set
            {
                if (user == value)
                    return;
                user = value;
                OnPropertyChanged(nameof(User));
            }
        }

        [Category("2. Database")]
        [PasswordPropertyText(true)]
        public string Password
        {
            get { return Coding.DecodeString(password); }
            set
            {
                if (value == Password)
                    return;
                password = Coding.EncodeString(value);
                OnPropertyChanged(nameof(Password));
            }
        }

        [Category("3. Additional")]
        public bool IntegratedSecurity
        {
            get { return integrSec; }
            set
            {
                if (integrSec == value)
                    return;
                integrSec = value;
                OnPropertyChanged(nameof(IntegratedSecurity));
            }
        }

        [Category("3. Additional")]
        public bool? Pool
        {
            get { return pool; }
            set
            {
                if (pool == value)
                    return;
                pool = value;
                OnPropertyChanged(nameof(Pool));
            }
        }

        [DefaultValue(false), Category("3. Additional")]
        public bool Encrypt
        {
            get { return encrypt; }
            set
            {
                encrypt = value;
                OnPropertyChanged(nameof(Encrypt));
            }
        }

        [Category("3. Additional")]
        public int TimeOut
        {
            get { return timeout; }
            set
            {
                if (timeout != value)
                {
                    timeout = value;
                    OnPropertyChanged(nameof(TimeOut));
                }
            }
        }

        [Category("3. Additional")]
        public IsolationLevel IsolationLevel
        {
            get { return System == DBSystem.SQLite ? global::System.Data.IsolationLevel.Unspecified : level; }
            set
            {
                if (level != value)
                {
                    level = value;
                    OnPropertyChanged(nameof(IsolationLevel));
                }
            }
        }

        [Category("3. Additional")]
        public string Extend
        {
            get { return extend; }
            set
            {
                if (extend == value)
                    return;
                extend = value;
                OnPropertyChanged(nameof(Extend));
            }
        }

        [Category("3. Additional")]
        public string Path
        {
            get { return path ?? Helper.GetDirectory(); }
            set
            {
                if (path != value)
                {
                    path = value;
                    OnPropertyChanged(nameof(Path));
                }
            }
        }

        public IDbConnection GetConnection()
        {
            lock (_locker)
            {
                foreach (var connection in Buffer)
                {
                    if (connection.State == ConnectionState.Closed)
                    {
                        try
                        {
                            connection.Open();
                        }
                        catch (Exception ex)
                        {
                            if (connection.State == ConnectionState.Open)
                                continue;
                            Buffer.Remove(connection);
                            Helper.OnException(ex);
                            throw (ex);
                        }
                        return connection;
                    }
                }
                Debug.WriteLine($"New Connection {Name} #{Buffer.Count}");
                var con = System.CreateConnection(this);
                Buffer.Add(con);
                con.Open();
                return con;
            }
        }

        public IDbCommand CreateCommand(string query = null, CommandType commandType = CommandType.Text)
        {
            return System.CreateCommand(this, query, commandType);
        }

        public bool CheckConnection()
        {
            try
            {
                var connection = GetConnection();
                connection.Close();
                return true;
            }
            catch (Exception ex)
            {
                Helper.OnException(ex);
            }
            return false;
        }

        public void ClearConnectionCache()
        {
            foreach (var con in Buffer)
            {
                if (con.State == ConnectionState.Open)
                    con.Close();
                con.Dispose();
            }
            Buffer.Clear();
            //DBService.Connections.Remove(this);
            //if (cs.RDBMS == RDBMS..SqLite)
            //    SqliteConnection.ClearAllPools();
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", name, database, System?.Name);
        }

        private static class Coding
        {
            public static byte[] EncodeString(string pass)
            {
                return EncodeBlock(Helper.EncodingByteString(pass));
            }

            public static string DecodeString(byte[] pass)
            {
                return pass == null ? null : Helper.DecodingByteString(Coding.DecodeBlock(pass));
            }

            public static byte[] EncodeBlock(byte[] block)
            {
                if (block == null)
                    return null;

                using (var memory = new MemoryStream())
                using (var des = new AesCryptoServiceProvider())
                using (var crypto = new CryptoStream(memory, des.CreateEncryptor(key, vector), CryptoStreamMode.Write))
                {
                    crypto.Write(block, 0, block.Length);
                    crypto.Close();
                    memory.Close();
                    return memory.ToArray();
                }
            }

            public static byte[] DecodeBlock(byte[] block)
            {
                if (block == null || block.Length == 0)
                    return new byte[] { };
                byte[] buffer = new byte[block.Length];

                try
                {
                    using (var memory = new MemoryStream(block))
                    using (var des = new AesCryptoServiceProvider())
                    using (var crypto = new CryptoStream(memory, des.CreateDecryptor(key, vector), CryptoStreamMode.Read))
                    {
                        crypto.Read(buffer, 0, buffer.Length);
                        crypto.Close();
                        memory.Close();

                    }
                }
                catch { buffer = null; }

                return buffer;
            }

            private static byte[] vector = Encoding.ASCII.GetBytes("Ke9d&4Jv@d0barh+");
            private static byte[] key = Encoding.ASCII.GetBytes("Jdv837rf;&G0dfj&");
        }

        public DBConnection Clone()
        {
            return new DBConnection
            {
                Name = Name,
                System = System,
                Host = Host,
                Port = port,
                DataBase = DataBase,
                Schema = Schema,
                User = User,
                Password = Password,
                IntegratedSecurity = IntegratedSecurity,
                Pool = Pool
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName]string property = null)
        {
            //Debug.WriteLine($"Connection set property {property} = {EmitInvoker.GetValue(GetType(), property, this)}");
            OnPropertyChanged(new PropertyChangedEventArgs(property));
        }

        public void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            ClearConnectionCache();
            PropertyChanged?.Invoke(this, args);
        }

        public DBTable<T> ExecuteTable<T>(string tableName, string query) where T : DBItem, new()
        {
            var schema = new DBSchema() { Name = "temp", Connection = this };
            var table = new DBTable<T>(tableName) { Schema = schema };
            using (var transaction = new DBTransaction(this, null, true))
            {
                table.Load(transaction.AddCommand(query));
            }
            return table;
        }

        public QResult ExecuteQResult(string query)
        {
            using (var transaction = new DBTransaction(this, null, true))
            {
                transaction.AddCommand(query);
                return transaction.ExecuteQResult();
            }
        }

        public QResult ExecuteQResult(IDbCommand command)
        {
            using (var transaction = new DBTransaction(this, null, true))
            {
                return transaction.ExecuteQResult(command);
            }
        }

        public List<List<KeyValuePair<string, object>>> ExecuteListPair(string query)
        {
            using (var transaction = new DBTransaction(this, null, true))
            {
                var list = new List<List<KeyValuePair<string, object>>>();
                using (var reader = transaction.ExecuteQuery(transaction.AddCommand(query), DBExecuteType.Reader) as IDataReader)
                {
                    int fCount = reader.FieldCount;
                    while (reader.Read())
                    {
                        var objects = new List<KeyValuePair<string, object>>(fCount);
                        for (int i = 0; i < fCount; i++)
                            objects.Add(new KeyValuePair<string, object>(reader.GetName(i), reader.GetValue(i)));
                        list.Add(objects);
                    }
                    reader.Close();
                }
                return list;
            }
        }

        public List<Dictionary<string, object>> ExecuteListDictionary(string query)
        {
            using (var transaction = new DBTransaction(this, null, true))
            {
                transaction.AddCommand(query);
                return transaction.ExecuteListDictionary();
            }
        }

        public object ExecuteQuery(string query, bool noTransaction = false, DBExecuteType type = DBExecuteType.Scalar)
        {
            if (string.IsNullOrEmpty(query))
                return null;
            using (var transaction = new DBTransaction(this, null, noTransaction))
            {
                var result = transaction.ExecuteQuery(transaction.AddCommand(query), type);
                transaction.Commit();
                return result;
            }
        }

        public IEnumerable<string> SplitGoQuery(string query)
        {
            var regex = new Regex(@"\s*go\s*(\n|$)", RegexOptions.IgnoreCase);
            foreach (var item in regex.Split(query))
            {
                if (item.Trim().Length == 0)
                {
                    continue;
                }
                yield return item;
            }
        }

        public List<object> ExecuteGoQuery(string query, bool noTransaction = true, DBExecuteType type = DBExecuteType.Scalar)
        {
            var result = new List<object>();
            foreach (var go in SplitGoQuery(query))
            {
                result.Add(ExecuteQuery(go, noTransaction, type));
            }
            return result;
        }

        public void Dispose()
        {
            ClearConnectionCache();
        }

        [Invoker(typeof(DBConnection), nameof(DBConnection.Name))]
        public class NameInvoker : Invoker<DBConnection, string>
        {
            public static readonly NameInvoker Instance = new NameInvoker();
            public override string Name => nameof(DBConnection.Name);

            public override bool CanWrite => true;

            public override string GetValue(DBConnection target) => target.Name;

            public override void SetValue(DBConnection target, string value) => target.Name = value;
        }

        [Invoker(typeof(DBConnection), nameof(DBConnection.SystemName))]
        public class SystemNameInvoker : Invoker<DBConnection, string>
        {
            public static readonly SystemNameInvoker Instance = new SystemNameInvoker();
            public override string Name => nameof(DBConnection.SystemName);

            public override bool CanWrite => true;

            public override string GetValue(DBConnection target) => target.SystemName;

            public override void SetValue(DBConnection target, string value) => target.SystemName = value;
        }

        [Invoker(typeof(DBConnection), nameof(DBConnection.Host))]
        public class HostInvoker : Invoker<DBConnection, string>
        {
            public static readonly HostInvoker Instance = new HostInvoker();
            public override string Name => nameof(DBConnection.Host);

            public override bool CanWrite => true;

            public override string GetValue(DBConnection target) => target.Host;

            public override void SetValue(DBConnection target, string value) => target.Host = value;
        }

        [Invoker(typeof(DBConnection), nameof(DBConnection.Port))]
        public class PortInvoker : Invoker<DBConnection, uint>
        {
            public static readonly PortInvoker Instance = new PortInvoker();
            public override string Name => nameof(DBConnection.Port);

            public override bool CanWrite => true;

            public override uint GetValue(DBConnection target) => target.Port;

            public override void SetValue(DBConnection target, uint value) => target.Port = value;
        }

        [Invoker(typeof(DBConnection), nameof(DBConnection.DataBase))]
        public class DataBaseInvoker : Invoker<DBConnection, string>
        {
            public static readonly DataBaseInvoker Instance = new DataBaseInvoker();
            public override string Name => nameof(DBConnection.DataBase);

            public override bool CanWrite => true;

            public override string GetValue(DBConnection target) => target.DataBase;

            public override void SetValue(DBConnection target, string value) => target.DataBase = value;
        }

        [Invoker(typeof(DBConnection), nameof(DBConnection.Schema))]
        public class SchemaInvoker : Invoker<DBConnection, string>
        {
            public static readonly SchemaInvoker Instance = new SchemaInvoker();
            public override string Name => nameof(DBConnection.Schema);

            public override bool CanWrite => true;

            public override string GetValue(DBConnection target) => target.Schema;

            public override void SetValue(DBConnection target, string value) => target.Schema = value;
        }

        [Invoker(typeof(DBConnection), nameof(DBConnection.User))]
        public class UserInvoker : Invoker<DBConnection, string>
        {
            public static readonly UserInvoker Instance = new UserInvoker();
            public override string Name => nameof(DBConnection.User);

            public override bool CanWrite => true;

            public override string GetValue(DBConnection target) => target.User;

            public override void SetValue(DBConnection target, string value) => target.User = value;
        }

        [Invoker(typeof(DBConnection), nameof(DBConnection.Password))]
        public class PasswordInvoker : Invoker<DBConnection, string>
        {
            public static readonly PasswordInvoker Instance = new PasswordInvoker();
            public override string Name => nameof(DBConnection.Password);

            public override bool CanWrite => true;

            public override string GetValue(DBConnection target) => target.Password;

            public override void SetValue(DBConnection target, string value) => target.Password = value;
        }

        [Invoker(typeof(DBConnection), nameof(DBConnection.Path))]
        public class PathInvoker : Invoker<DBConnection, string>
        {
            public static readonly PathInvoker Instance = new PathInvoker();
            public override string Name => nameof(DBConnection.Path);

            public override bool CanWrite => true;

            public override string GetValue(DBConnection target) => target.Path;

            public override void SetValue(DBConnection target, string value) => target.Path = value;
        }

    }
}
