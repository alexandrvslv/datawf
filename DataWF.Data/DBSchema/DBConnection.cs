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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using PathHelper = System.IO.Path;

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

    [InvokerGenerator(Instance = true)]
    public partial class DBConnection : INotifyPropertyChanged, IDisposable
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
        private readonly object _locker = new object();
        private string systemName;
        private DBSystem system;
        internal HashSet<IDbConnection> Buffer = new HashSet<IDbConnection>();
        private string path;
        private FileStorage fileStorage;
        private byte dataBaseId;

        public DBConnection()
        { }

        public DBConnection(string name)
        {
            Name = name;
        }

        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged();
                }
            }
        }

        [Browsable(false)]
        public string SystemName
        {
            get => systemName;
            set
            {
                systemName = value;
                system = GetSystem();
                OnPropertyChanged();
            }
        }

        [XmlIgnore, JsonIgnore, Category("1. Host")]
        public DBSystem System
        {
            get => system ?? (system = GetSystem());
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
            get => host;
            set
            {
                if (host == value)
                    return;
                host = value;
                OnPropertyChanged();
            }
        }

        [Category("1. Host")]
        public uint Port
        {
            get => port;
            set
            {
                if (port == value)
                    return;
                port = value;
                OnPropertyChanged();
            }
        }

        [Category("2. Database")]
        public string DataBase
        {
            get => database;
            set
            {
                if (database == value)
                    return;
                database = value;
                OnPropertyChanged();
            }
        }

        [Category("2. Database")]
        public string Schema
        {
            get => schem;
            set
            {
                if (schem == value)
                    return;
                schem = value;
                OnPropertyChanged();
            }
        }

        [Category("2. Database")]
        public string User
        {
            get => user;
            set
            {
                if (user == value)
                    return;
                user = value;
                OnPropertyChanged();
            }
        }

        [Category("2. Database")]
        [PasswordPropertyText(true)]
        public string Password
        {
            get => Coding.DecodeString(password);
            set
            {
                if (value == Password)
                    return;
                password = Coding.EncodeString(value);
                OnPropertyChanged();
            }
        }

        [Category("3. Additional")]
        public bool IntegratedSecurity
        {
            get => integrSec;
            set
            {
                if (integrSec == value)
                    return;
                integrSec = value;
                OnPropertyChanged();
            }
        }

        [Category("3. Additional")]
        public bool? Pool
        {
            get => pool;
            set
            {
                if (pool == value)
                    return;
                pool = value;
                OnPropertyChanged();
            }
        }

        [DefaultValue(false), Category("3. Additional")]
        public bool Encrypt
        {
            get => encrypt;
            set
            {
                encrypt = value;
                OnPropertyChanged();
            }
        }

        [Category("3. Additional")]
        public int TimeOut
        {
            get => timeout;
            set
            {
                if (timeout != value)
                {
                    timeout = value;
                    OnPropertyChanged();
                }
            }
        }

        [Category("3. Additional")]
        public IsolationLevel IsolationLevel
        {
            get => System == DBSystem.SQLite ? global::System.Data.IsolationLevel.Unspecified : level;
            set
            {
                if (level != value)
                {
                    level = value;
                    OnPropertyChanged();
                }
            }
        }

        [Category("3. Additional")]
        public string Extend
        {
            get => extend;
            set
            {
                if (extend == value)
                    return;
                extend = value;
                OnPropertyChanged();
            }
        }

        public string GetFilesPath()
        {
            return PathHelper.Combine(Path, "Files");
        }

        public string GetFilePath(long id)
        {
            var path = GetFilesPath();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return PathHelper.Combine(path, id.ToString("D20"));
        }

        [Category("3. Additional")]
        public string Path
        {
            get => path ?? Helper.GetDirectory();
            set
            {
                if (path != value)
                {
                    path = value;
                    OnPropertyChanged();
                }
            }
        }

        public FileStorage FileStorage
        {
            get => fileStorage;
            set
            {
                if (fileStorage != value)
                {
                    fileStorage = value;
                    OnPropertyChanged();
                }
            }
        }

        //https://www.enterprisedb.com/blog/generated-primary-and-foreign-keys-distributed-databases
        public byte DataBaseId
        {
            get => dataBaseId;
            set
            {
                if (dataBaseId != value)
                {
                    dataBaseId = value;
                    OnPropertyChanged();
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

        public bool CheckConnection(bool throwException = false)
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
                if (throwException)
                {
                    throw;
                }
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

            private static readonly byte[] vector = Encoding.ASCII.GetBytes("Ke9d&4Jv@d0barh+");
            private static readonly byte[] key = Encoding.ASCII.GetBytes("Jdv837rf;&G0dfj&");
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

        public void OnPropertyChanged([CallerMemberName] string property = null)
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
                return transaction.ExecuteQResult(transaction.AddCommand(query));
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
                var command = transaction.AddCommand(query);
                return transaction.ExecuteListDictionary(command);
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

        public async Task<object> ExecuteQueryAsync(string query, bool noTransaction = false, DBExecuteType type = DBExecuteType.Scalar)
        {
            if (string.IsNullOrEmpty(query))
                return null;
            using (var transaction = new DBTransaction(this, null, noTransaction))
            {
                var result = await transaction.ExecuteQueryAsync(transaction.AddCommand(query), type);
                transaction.Commit();
                return result;
            }
        }

        public object ExecuteQuery(IDbCommand command, bool noTransaction = false, DBExecuteType type = DBExecuteType.Scalar)
        {
            if (command == null)
                return null;
            using (var transaction = new DBTransaction(this, null, noTransaction))
            {
                var result = transaction.ExecuteQuery(command, type);
                transaction.Commit();
                return result;
            }
        }

        public async Task<object> ExecuteQueryAsync(IDbCommand command, bool noTransaction = false, DBExecuteType type = DBExecuteType.Scalar)
        {
            if (command == null)
                return null;
            using (var transaction = new DBTransaction(this, null, noTransaction))
            {
                var result = await transaction.ExecuteQueryAsync(command, type);
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

        public async Task<List<object>> ExecuteGoQueryAsync(string query, bool noTransaction = true, DBExecuteType type = DBExecuteType.Scalar)
        {
            var result = new List<object>();
            foreach (var go in SplitGoQuery(query))
            {
                result.Add(await ExecuteQueryAsync(go, noTransaction, type));
            }
            return result;
        }

        public void Dispose()
        {
            ClearConnectionCache();
        }

    }

    public enum FileStorage
    {
        FileTable,
        FileSystem,
        DatabaseSystem,
        ExternalSystem
    }
}
