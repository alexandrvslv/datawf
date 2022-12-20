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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class ReplicationService
    {
        private static readonly object loadLock = new object();
        private readonly ConcurrentDictionary<DBTransaction, List<RSItem>> buffer = new ConcurrentDictionary<DBTransaction, List<RSItem>>();

        private readonly CancellationTokenSource synchCancel = new CancellationTokenSource();

        public ReplicationService(ReplicationSettings settings, ISocketService socketService, DBProvider provider)
        {
            Settings = settings;
            SocketService = socketService;
            Provider = provider;
        }

        public ReplicationSettings Settings { get; }
        public ISocketService SocketService { get; }
        public DBProvider Provider { get; }

        public virtual void Dispose()
        {
            Stop().GetAwaiter().GetResult();
            SocketService.Dispose();
        }

        public void Start()
        {
            SocketService.Address = Settings.Instance.UrlValue;
            SocketService.StartListener(100);
            SocketService.ClientConnect += OnConnectionEstablished;
            DBService.AddItemUpdated(OnItemUpdate);
            DBService.AddTransactionCommit(OnTransactionCommit);
        }

        private void OnConnectionEstablished(object sender, SocketConnectionArgs e)
        {
            var instance = Settings.GetInstance(e.Connection.Address);
            if (instance == null)
            {
                this.Log($"Unknown Address: {e.Connection.Address}", StatusType.Warning);
                e.Connection.Dispose();
            }
            instance.Connection = e.Connection;
            instance.Active = true;
        }

        public async Task Stop()
        {
            SocketService.StopListener();

            DBService.RemoveItemUpdated(OnItemUpdate);
            DBService.RemoveTransactionCommit(OnTransactionCommit);
            await Broadcast(new SMRequest
            {
                Id = SMBase.NewId(),
                Url = Settings.Instance.Url,
                RequestType = SMRequestType.Logout,
                Data = "By"
            }, false);
        }

        public async Task Synch()
        {
            await SignIn();
            foreach (var schema in Settings.Schems)
            {
                schema.Initialize(Provider);
            }
            foreach (var instance in Settings.Instances)
            {
                instance.Provider = Provider;
                if (instance.Active ?? false)
                {
                    await Synch(instance);
                }
            }
        }

        public async ValueTask SignIn(ManualResetEventSlim loginEvent = null)
        {
            if (!Settings.Instances.Any(p => !p.Active.GetValueOrDefault()))
            {
                return;
            }
            loginEvent.Reset();
            await Broadcast(new SMRequest
            {
                Id = SMBase.NewId(),
                Url = Settings.Instance.Url,
                RequestType = SMRequestType.Login,
                Data = "Hi"
            }, true);
            loginEvent.Wait();
        }

        private async Task Synch(RSInstance instance)
        {
            foreach (var schema in Settings.Schems)
            {
                await schema.Synch(instance);
            }
        }

        private async ValueTask OnTransactionCommit(DBTransaction transaction)
        {
            if (transaction.State == DBTransactionState.Rollback)
            {
                buffer.TryRemove(transaction, out _);
            }
            else
            {
                if (buffer.TryRemove(transaction, out var list))
                    await Broadcast(transaction, list);
            }
        }

        private ValueTask OnItemUpdate(DBItemEventArgs arg)
        {
            if (arg.Transaction.Replication)
            {
                return default(ValueTask);
            }

            var item = arg.Item;

            if (item.Table.Type == DBTableType.Table && CheckTable(item.Table))
            {
                var type = (arg.State & DBUpdateState.Delete) == DBUpdateState.Delete ? DBLogType.Delete
                    : (arg.State & DBUpdateState.Insert) == DBUpdateState.Insert ? DBLogType.Insert
                    : DBLogType.Update;
                var replica = new RSItem()
                {
                    Command = type,
                    UserId = arg.User?.Id ?? 0,
                    Value = item,
                    Columns = arg.Columns
                };
                var items = buffer.GetOrAdd(arg.Transaction, new List<RSItem>());
                items.Add(replica);
            }
            return default(ValueTask);
        }

        private bool CheckTable(IDBTable table)
        {
            var srSchema = Settings.GetSchema(table.Schema);
            if (srSchema == null)
                return false;
            return srSchema.GetRSTable(table) != null;
        }

        public async Task<bool> Broadcast(DBTransaction transaction, List<RSItem> items)
        {
            var message = new SMNotify
            {
                Id = SMBase.NewId(),
                Url = Settings.Instance.Url,
                Data = new RSTransaction { Connection = transaction.DbConnection.Name, Items = items }
            };

            return await Broadcast(message);
        }

        public async Task<bool> Broadcast<T>(T message, bool signIn = false) where T : SMBase
        {
            bool sended = false;
            foreach (var item in Settings.Instances)
            {
                if (signIn && item.Active.GetValueOrDefault(false))
                    continue;
                if (await CheckAddress(Settings.Instance, item, signIn))
                    sended = await item.Send(message);
            }
            return sended;
        }

        private async ValueTask<bool> CheckAddress(RSInstance item, RSInstance address, bool signIn = false)
        {
            if (signIn && item.Active == null)
            {
                try
                {
                    if (address.Connection == null)
                    {
                        address.Connection = await SocketService.CreateConnection(address.UrlValue);
                    }
                    else if (!address.Connection.Connected)
                    {
                        await address.Connection.Connect();
                    }
                }
                catch
                {
                    Helper.Log("Replication", "Connection Fail", $"Address: {address.Url}", StatusType.Warning);
                }
            }
            return address.Connection?.Connected ?? false;
        }

        public object GenerateSchemaInfo(string schemaName)
        {
            throw new NotImplementedException();
        }

        public object GenerateFile(string schemaName, long v)
        {
            throw new NotImplementedException();
        }

        public object GenerateTableDiff(string schemaName, string tableName, DateTime? stamp)
        {
            var table = Provider.GetDBSchema(schemaName)?.GetTable(tableName);
            if (table == null)
                throw new Exception("No such Schema/Table");
            return table.GetReplicateItems(stamp);
        }

    }

    public class ReplicationEventArgs : EventArgs
    {
        public ReplicationEventArgs(List<RSItem> data)
        {
            Data = data;
        }

        public List<RSItem> Data { get; }
    }
}
