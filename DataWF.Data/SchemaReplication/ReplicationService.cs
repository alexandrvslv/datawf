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
    public class ReplicationService : TcpServer
    {
        private static readonly object loadLock = new object();
        private readonly ConcurrentDictionary<DBTransaction, List<RSItem>> buffer = new ConcurrentDictionary<DBTransaction, List<RSItem>>();
        private readonly ConcurrentDictionary<long, SMRequest> requests = new ConcurrentDictionary<long, SMRequest>();
        private readonly ManualResetEventSlim loginEvent = new ManualResetEventSlim(false);
        private readonly CancellationTokenSource synchCancel = new CancellationTokenSource();
        private readonly BinarySerializer serializer = new BinarySerializer();

        public ReplicationService(ReplicationSettings settings) : base()
        {
            Settings = settings;
        }

        public ReplicationSettings Settings { get; }

        public override void Dispose()
        {
            Stop().GetAwaiter().GetResult();
            base.Dispose();
        }

        public void Start()
        {
            Point = Settings.Instance.EndPoint;
            StartListener(100);

            DBService.AddItemUpdated(OnItemUpdate);
            DBService.AddTransactionCommit(OnTransactionCommit);

            _ = SignIn();
        }

        public async Task Stop()
        {
            StopListener();

            DBService.RemoveItemUpdated(OnItemUpdate);
            DBService.RemoveTransactionCommit(OnTransactionCommit);
            await Broadcast(new SMRequest
            {
                Id = SMBase.NewId(),
                EndPoint = Point,
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
                    EndPoint = Point,
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
                EndPoint = Point,
                Data = new RSTransaction { Connection = transaction.DbConnection.Name, Items = items }
            };

            return await Broadcast(message);
        }

        public async Task<bool> Broadcast<T>(T message, bool checkState = false) where T : SMBase
        {
            if (!Settings.Instances.Any(p => CheckAddress(p, null, checkState)))
            {
                return false;
            }

            bool sended = false;
            foreach (var item in Settings.Instances)
            {
                if(CheckAddress(Settings.Instance, item, checkState))
                sended = await item.Send(message, this);
            }
            return sended;
        }

       

        private bool CheckAddress(RSInstance item, RSInstance address, bool checkState = false)
        {
            return (address == null || item.Equals(address))
                && ((item.Active ?? false) || (item.Active == null && checkState));
        }

        protected override async Task OnDataLoadStart(TcpStreamEventArgs arg)
        {
            //await base.OnDataLoadStart(arg);
            try
            {
                SMBase message = null;
                using (var stream = arg.ReaderStream)
                {
                    message = (SMBase)serializer.Deserialize(stream, null);
                }
                //message.Caller = arg.Client;
                switch (message.Type)
                {
                    case SMType.Request:
                        await ProcessRequest((SMRequest)message, arg);
                        break;
                    case SMType.Response:
                        await ProcessResponse((SMResponce)message, arg);
                        break;
                    case SMType.Notify:
                        if (message.Data is RSTransaction transaction)
                        {
                            await LoadTransaction(transaction);
                        }
                        break;
                }
                arg.CompleteRead();
                await arg.ReleasePipe();
            }
            catch (Exception ex)
            {
                arg.CompleteRead(ex);
                await arg.ReleasePipe(ex);
                OnDataException(new TcpExceptionEventArgs(arg, ex));
            }
        }

        private async ValueTask ProcessResponse(SMResponce message, TcpStreamEventArgs arg)
        {
            var sender = Settings.Instances.FirstOrDefault(p => p.EndPointName == message.EndPoint.ToString());
            if (sender == null)
                return;
            if (message.RequestId is long requestId
                && requests.TryGetValue(requestId, out var request))
            {
                request.Responce = message;
                requestEvent.Set();
            }
            switch (message.ResponceType)
            {
                case SMResponceType.Confirm:
                    sender.Active = true;
                    sender.TcpSocket = arg.TcpSocket;
                    break;
                case SMResponceType.Data:
                    if (message.Data is RSResult result)
                    {
                        switch (result.Type)
                        {
                            case RSQueryType.SchemaInfo:
                                break;
                            case RSQueryType.SynchTable:
                                break;
                            case RSQueryType.SynchSequence:
                                break;
                            case RSQueryType.SynchFile:
                                break;
                        }
                    }
                    break;
            }
        }

        protected virtual async ValueTask ProcessRequest(SMRequest message, TcpStreamEventArgs arg)
        {
            var sender = Settings.Instances.FirstOrDefault(p => p.EndPointName == message.EndPoint.ToString());
            if (sender == null)
                return;

            switch (message.RequestType)
            {
                case SMRequestType.Login:
                    sender.Active = true;
                    sender.TcpSocket = arg.TcpSocket;
                    await Send(new SMResponce
                    {
                        Id = SMBase.NewId(),
                        EndPoint = Point,
                        RequestId = message.Id,
                        ResponceType = SMResponceType.Confirm,
                        Data = "Hi"
                    }, sender);
                    break;
                case SMRequestType.Logout:
                    sender.Active = false;
                    sender.TcpSocket = null;
                    break;
                case SMRequestType.Data:
                    //if(!sender.Active)
                    try
                    {
                        object data = null;

                        if (message.Data is RSQuery query)
                        {
                            switch (query.Type)
                            {
                                case RSQueryType.SchemaInfo:
                                    data = GenerateSchemaInfo(query.SchemaName);
                                    break;
                                case RSQueryType.SynchTable:
                                    data = GenerateTableDiff(query.SchemaName, query.ObjectId, query.Stamp);
                                    break;
                                case RSQueryType.SynchFile:
                                    data = GenerateFile(query.SchemaName, long.TryParse(query.ObjectId, out var id) ? id : -1L);
                                    break;
                            }
                        }
                        var responce = new SMResponce
                        {
                            Id = SMBase.NewId(),
                            EndPoint = Point,
                            RequestId = message.Id,
                            ResponceType = SMResponceType.Data,
                            Data = data
                        };
                        await Send(responce, sender);
                        if (data is Stream stream)
                        {
                            stream.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        var responce = new SMResponce
                        {
                            Id = SMBase.NewId(),
                            EndPoint = Point,
                            RequestId = message.Id,
                            ResponceType = SMResponceType.Decline,
                            Data = ex.Message
                        };
                        await Send(responce, sender);
                    }

                    break;
            }
        }

        private object GenerateSchemaInfo(string schemaName)
        {
            throw new NotImplementedException();
        }

        private object GenerateFile(string schemaName, long v)
        {
            throw new NotImplementedException();
        }

        private object GenerateTableDiff(string schemaName, string tableName, DateTime? stamp)
        {
            var table = DBService.Schems[schemaName]?.Tables[tableName];
            if (table == null)
                throw new Exception("No such Schema/Table");
            return table.GetReplicateItems(stamp);
        }

        public static async Task LoadTransaction(RSTransaction srTransaction)
        {
            using (var transaction = new DBTransaction(DBService.Connections[srTransaction.Connection], null, true)
            { Replication = true })
            {
                try
                {
                    foreach (var item in srTransaction.Items)
                    {
                        switch (item.Command)
                        {
                            case DBLogType.Insert:
                            case DBLogType.Update:
                                await item.Value.Table.SaveItem(item.Value, transaction);
                                break;
                            case DBLogType.Delete:
                                await item.Value.Delete(transaction);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Helper.OnException(ex);
                }
            }
        }

        protected override async ValueTask OnDataSend(TcpStreamEventArgs arg)
        {
            await base.OnDataSend(arg);
        }

        protected override void OnDataException(SocketExceptionArgs arg)
        {
            base.OnDataException(arg);
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
