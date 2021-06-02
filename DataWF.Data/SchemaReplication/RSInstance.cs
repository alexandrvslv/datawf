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
using System.Net;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DataWF.Data
{
    [InvokerGenerator]
    public partial class RSInstance : DefaultItem, IEquatable<RSInstance>
    {
        private readonly ManualResetEventSlim requestEvent = new ManualResetEventSlim(false);
        private readonly ConcurrentDictionary<long, SMRequest> requests = new ConcurrentDictionary<long, SMRequest>();
        private readonly BinarySerializer serializer = new BinarySerializer();

        private static readonly int timeOut = 60000;
        private ISocketConnection connection;

        public string Url { get; set; } = "tcp://localhost:50001";

        [XmlIgnore, JsonIgnore]
        public bool? Active { get; internal set; }

        [XmlIgnore, JsonIgnore]
        public ISocketConnection Connection
        {
            get => connection;
            internal set
            {
                connection = value;
                connection.ReceiveStart = OnReceiveStart;
                OnPropertyChanged();
            }
        }

        [XmlIgnore, JsonIgnore]
        public ReplicationService Service { get; set; }
        public override bool Equals(object obj)
        {
            return Equals(obj as RSInstance);
        }

        public bool Equals(RSInstance other)
        {
            return other != null &&
                   string.Equals(Url, other.Url, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<string>.Default.GetHashCode(Url);
        }
        public async Task<SMResponce> Request(SMRequest request)
        {
            if (!(Active ?? false))
                throw new Exception("Inactive recipient");

            requestEvent.Reset();
            if (await Send(request))
            {
                requestEvent.Wait(timeOut);
                return request.Responce;
            }
            return null;
        }

        public async Task<bool> Send<T>(T message) where T : SMBase
        {
            var sended = await Connection.SendT(message);
            if (sended)
            {
                if (message is SMRequest request)
                {
                    requests[request.Id] = request;
                }
            }
            return false;
        }


        protected async ValueTask OnReceiveStart(SocketStreamArgs arg)
        {
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
                            await ProcessTransaction(transaction);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Service.SocketService.OnDataException(new SocketExceptionArgs(arg, ex));
            }
        }

        protected virtual async ValueTask ProcessRequest(SMRequest message, SocketStreamArgs arg)
        {
            if (Url != message.Url)
                throw new Exception("Wrong Url");

            switch (message.RequestType)
            {
                case SMRequestType.Login:
                    Active = true;
                    Connection = arg.Connection;
                    await Send(new SMResponce
                    {
                        Id = SMBase.NewId(),
                        Url = Service.Settings.Instance.Url,
                        RequestId = message.Id,
                        ResponceType = SMResponceType.Confirm,
                        Data = "Hi"
                    });
                    break;
                case SMRequestType.Logout:
                    Active = false;
                    await Connection.Disconnect();
                    Connection = null;
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
                                    data = Service.GenerateSchemaInfo(query.SchemaName);
                                    break;
                                case RSQueryType.SynchTable:
                                    data = Service.GenerateTableDiff(query.SchemaName, query.ObjectId, query.Stamp);
                                    break;
                                case RSQueryType.SynchFile:
                                    data = Service.GenerateFile(query.SchemaName, long.TryParse(query.ObjectId, out var id) ? id : -1L);
                                    break;
                            }
                        }
                        var responce = new SMResponce
                        {
                            Id = SMBase.NewId(),
                            Url = Service.Settings.Instance.Url,
                            RequestId = message.Id,
                            ResponceType = SMResponceType.Data,
                            Data = data
                        };
                        await Send(responce);
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
                            Url = Service.Settings.Instance.Url,
                            RequestId = message.Id,
                            ResponceType = SMResponceType.Decline,
                            Data = ex.Message
                        };
                        await Send(responce);
                    }

                    break;
            }
        }

        private async ValueTask ProcessResponse(SMResponce message, SocketStreamArgs arg)
        {
            //Service.Settings.Instances
            if (message.RequestId is long requestId
                && requests.TryGetValue(requestId, out var request))
            {
                request.Responce = message;
                requestEvent.Set();
            }
            switch (message.ResponceType)
            {
                case SMResponceType.Confirm:
                    Active = true;
                    Connection = arg.Connection;
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

        public static async Task ProcessTransaction(RSTransaction srTransaction)
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
    }
}
