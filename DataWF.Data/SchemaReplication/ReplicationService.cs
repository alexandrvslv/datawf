﻿//  The MIT License (MIT)
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

        private readonly ManualResetEventSlim loginEvent = new ManualResetEventSlim(false);
        private readonly CancellationTokenSource synchCancel = new CancellationTokenSource();

        public ReplicationService(ReplicationSettings settings, ISocketService socketService)
        {
            Settings = settings;
            SocketService = socketService;
        }

        public ReplicationSettings Settings { get; }
        public ISocketService SocketService { get; }

        public virtual void Dispose()
        {
            Stop().GetAwaiter().GetResult();
            SocketService.Dispose();
        }

        public void Start()
        {
            SocketService.Address = new Uri(Settings.Instance.Url);
            SocketService.StartListener(100);

            DBService.AddItemUpdated(OnItemUpdate);
            DBService.AddTransactionCommit(OnTransactionCommit);

            _ = SignIn();
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
                schema.Initialize();
            }
            foreach (var instance in Settings.Instances)
            {
                if (instance.Active ?? false)
                {
                    await Synch(instance);
                }
            }
        }

        public async Task SignIn()
        {
            if (Settings.Instance.Active == false)
            {
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

        public async Task<bool> Broadcast<T>(T message, bool checkState = false) where T : SMBase
        {
            bool sended = false;
            foreach (var item in Settings.Instances)
            {
                if (await CheckAddress(Settings.Instance, item, checkState))
                    sended = await item.Send(message);
            }
            return sended;
        }

        private async ValueTask<bool> CheckAddress(RSInstance item, RSInstance address, bool checkState = false)
        {
            var isChecked = (address == null || item.Equals(address))
                && ((item.Active ?? false) || (item.Active == null && checkState));
            if (isChecked && address.Connection == null)
            {
                address.Connection = await SocketService.CreateConnection(new Uri(address.Url));
            }
            return isChecked && (address.Connection?.Connected ?? false);
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
            var table = DBService.Schems[schemaName]?.Tables[tableName];
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
