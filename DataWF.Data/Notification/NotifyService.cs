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
    public class NotifyService : UdpServer
    {
        public static List<NotifyDBTable> Dequeu(ConcurrentQueue<NotifyDBItem> buffer)
        {
            var length = buffer.Count > 200 ? 200 : buffer.Count;
            var map = new Dictionary<Type, NotifyDBTable>();

            for (int i = 0; i < length; i++)
            {
                if (buffer.TryDequeue(out var item))
                {
                    var itemType = item.Value.GetType();
                    if (!map.TryGetValue(itemType, out var typeTable))
                    {
                        map[itemType] = typeTable = new NotifyDBTable { Type = itemType, Table = (DBTable)item.Value.Table };
                    }
                    if (!typeTable.Items.Any(p => p.Id.Equals(item.Id)))
                    {
                        typeTable.Items.Add(item);
                    }
                }
            }
            var list = map.Values.ToList();
            list.Sort();
            foreach (var table in list)
            {
                table.Items.Sort();
            }

            return list;
        }

        private readonly object loadLock = new object();

        private IInstance instance;
        private readonly ConcurrentQueue<NotifyDBItem> buffer = new ConcurrentQueue<NotifyDBItem>();
        private readonly ManualResetEventSlim runEvent = new ManualResetEventSlim(false);
        private readonly BinarySerializer serializer = new BinarySerializer();
        private const int timer = 2000;

        private IPEndPoint endPoint;

        public NotifyService(NotifySettings settings) : base()
        {
            Settings = settings;
        }

        public InstanceTable InstanceTable { get; set; }

        public IUserIdentity User { get; private set; }

        public NotifySettings Settings { get; }

        public event EventHandler<NotifyEventArgs> SendChanges;

        public event Action<SMBase> MessageLoad;

        public override async void Dispose()
        {
            await Logout();
            base.Dispose();
        }

        public async Task Start()
        {
            StartListener();
            endPoint = new IPEndPoint(SocketHelper.GetInterNetworkIPs().First(), ListenerEndPoint.Port);
            instance = await InstanceTable.GetByNetId(endPoint, true, User);

            byte[] temp = instance.EndPoint.GetBytes();
            InstanceTable.Load();

            await SendMessage(new SMRequest { Id = SMBase.NewId(), RequestType = SMRequestType.Login, Data = instance.Id }, null, true);

            DBService.AddRowAccept(OnAccept);
            runEvent.Reset();

            _ = SendChangesRunner();
        }

        public async ValueTask Logout()
        {
            if (instance == null)
                return;
            DBService.RemoveRowAccept(OnAccept);
            runEvent.Set();

            await SendMessage(new SMRequest { Id = SMBase.NewId(), RequestType = SMRequestType.Logout, Data = "By by!" });
            StopListener();
            instance.Delete();
            await instance.Save(User);
            instance = null;
        }

        public async ValueTask SendNotify(List<NotifyDBTable> items, IInstance address = null)
        {
            SendChanges?.Invoke(this, new NotifyEventArgs(items));
            if (!((IEnumerable<Instance>)InstanceTable).Any(p => CheckAddress(p, address)))
            {
                return;
            }
            var message = new SMNotify
            {
                Id = SMBase.NewId(),
                EndPoint = endPoint,
                Data = items
            };
            var buffer = serializer.Serialize(message);
            foreach (IInstance item in InstanceTable)
            {
                if (CheckAddress(item, address))
                {
                    await Send(buffer, item.EndPoint, item);
                }
            }
        }

        public async ValueTask SendMessage<T>(T message, IInstance address = null, bool checkState = false) where T : SMBase
        {
            var buffer = serializer.Serialize<T>(message);

            foreach (IInstance item in InstanceTable)
            {
                if (CheckAddress(item, address, checkState))
                {
                    await Send(buffer, item.EndPoint, item);
                }
            }
        }

        protected override async ValueTask OnDataSend(UdpServerEventArgs arg)
        {
            await base.OnDataSend(arg);
            if (arg.Tag is IInstance instance)
            {
                instance.SendCount++;
                instance.SendLength = arg.Length;
            }
        }

        protected override void OnDataException(UdpServerEventArgs arg)
        {
            base.OnDataException(arg);
            if (arg.Tag is IInstance instance)
            {
                instance.Active = false;
            }
        }

        private bool CheckAddress(IInstance item, IInstance address, bool checkState = false)
        {
            if (checkState)
            {
                item.Active = null;
            }
            return (address == null || item == address)
                && (checkState || item.Active == true)
                && item.EndPoint != null
                && !item.EndPoint.Equals(endPoint)
                && !IPAddress.Loopback.Equals(item.EndPoint.Address);
        }

        private ValueTask OnAccept(DBItemEventArgs arg)
        {
            var item = arg.Item;

            if (!(item is DBLogItem) && item.Table.Type == DBTableType.Table && item.Table.IsLoging)
            {
                var type = (arg.State & DBUpdateState.Delete) == DBUpdateState.Delete ? DBLogType.Delete
                    : (arg.State & DBUpdateState.Insert) == DBUpdateState.Insert ? DBLogType.Insert
                    : DBLogType.Update;
                buffer.Enqueue(new NotifyDBItem()
                {
                    Value = item,
                    Id = item.PrimaryId,
                    Command = type,
                    UserId = arg.User?.Id ?? 0
                });
            }
            return default;
        }

        protected override async ValueTask OnDataLoad(UdpServerEventArgs arg)
        {
            await base.OnDataLoad(arg);
            //arg.Point

            try
            {
                var message = serializer.Deserialize<SMBase>(arg.Data, null);
                if (message != null)
                { await OnMessageLoad(message, arg); }
            }
            catch (Exception e)
            {
                Helper.OnException(e);
            }
        }

        protected virtual async ValueTask OnMessageLoad(SMBase message, UdpServerEventArgs arg)
        {
            var sender = ((IEnumerable<Instance>)InstanceTable).FirstOrDefault(p => p.EndPoint == message.EndPoint);
            if (sender == null)
                return;
            sender.ReceiveCount++;
            sender.ReceiveLength += arg.Length;
            if (message is SMRequest request)
            {
                switch (request.RequestType)
                {
                    case (SMRequestType.Login):
                        sender.Active = true;
                        await SendMessage(new SMResponce
                        {
                            Id = SMBase.NewId(),
                            RequestId = request.Id,
                            EndPoint = endPoint,
                            ResponceType = SMResponceType.Confirm,
                            Data = "Hi!"
                        }, sender);
                        break;
                    case (SMRequestType.Logout):
                        sender.Detach();
                        break;
                    case (SMRequestType.Data):
                        break;
                }
            }
            else if (message is SMNotify notify)
            {
                if (notify.Data is List<NotifyDBTable> list)
                {
                    LoadNotify(list);
                }
            }
            MessageLoad?.Invoke(message);
        }

        private async Task SendChangesRunner()
        {
            while (!runEvent.Wait(timer))
            {
                try
                {
                    if (buffer.Count == 0)
                        continue;
                    List<NotifyDBTable> list = Dequeu(buffer);

                    await SendNotify(list);
                }
                catch (Exception e)
                {
                    Helper.OnException(e);
                }
            }
        }

        protected void LoadNotify(List<NotifyDBTable> list)
        {
            lock (loadLock)
            {
                foreach (var typeTable in list)
                {
                    if (typeTable.Type == null)
                    {
                        continue;
                    }
                    var primaryKey = typeTable.Table.PrimaryKey;
                    using (var transaction = new DBTransaction(typeTable.Table, null, true))
                    {
                        foreach (var item in typeTable.Items)
                        {
                            switch (item.Command)
                            {
                                case DBLogType.Insert:
                                    primaryKey.LoadByKey(item.Id, DBLoadParam.Load, null, transaction);
                                    break;
                                case DBLogType.Update:
                                    var record = primaryKey.LoadByKey(item.Id, DBLoadParam.None);
                                    if (record != null)
                                    {
                                        typeTable.Table.ReloadItem(item.Id, DBLoadParam.Load, transaction);
                                    }
                                    break;
                                case DBLogType.Delete:
                                    var toDelete = primaryKey.LoadByKey(item.Id, DBLoadParam.None);
                                    if (item != null)
                                    {
                                        typeTable.Table.Remove(toDelete);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }

    public class NotifyEventArgs : EventArgs
    {
        public NotifyEventArgs(List<NotifyDBTable> data)
        {
            Data = data;
        }

        public List<NotifyDBTable> Data { get; }
    }
}
