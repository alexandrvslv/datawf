using DataWF.Data;
using DataWF.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Timers;
using System.Net;
using System.Threading.Tasks;
using System.IO;

namespace DataWF.Module.Common
{
    public static class EndPointExtension
    {
        public static byte[] GetBytes(this IPEndPoint endPoint)
        {
            var result = new List<byte>(endPoint.Address.GetAddressBytes());
            result.AddRange(BitConverter.GetBytes(endPoint.Port));
            return result.ToArray();
        }

        public static IPEndPoint GetEndPoint(this byte[] buffer)
        {
            var temp = new byte[buffer.Length - 4];
            Array.Copy(buffer, 0, temp, 0, temp.Length);
            var address = new IPAddress(temp);
            return new IPEndPoint(address, BitConverter.ToInt32(buffer, temp.Length + 1));
        }
    }


    public class NotifyService : UdpServer
    {
        public static void Intergate(Action<EndPointMessage> onLoad)
        {
            var service = new NotifyService();
            service.MessageLoad += onLoad;
            service.StartListener();
            service.Login();
        }

        private static void ServiceLoaded(EndPointMessage obj)
        {
            var instance = Instance.DBTable.LoadById(obj.Sender);
            if (instance == null || instance.IsCurrent)
                return;
            if (obj.Type == SocketMessageType.Login)
            {
                //SetStatus(user + " login!");
            }
            else if (obj.Type == SocketMessageType.Hello)
            {
                //SetStatus(user + " is online!");
            }
            else if (obj.Type == SocketMessageType.Logout)
            {
                //SetStatus(user + " was logout!");
            }
        }

        class MessageItem
        {
            public DBTable Table;
            public DBLogType Type;
            public object Id;
        }

        private struct LoadTask
        {
            public DBTable Table;
            public bool Refresh;
            public object Id;
        }

        public static NotifyService Default;

        private static object loadLock = new Queue<LoadTask>();

        private Instance instance;
        private ConcurrentBag<MessageItem> buffer = new ConcurrentBag<MessageItem>();
        private ManualResetEvent runEvent = new ManualResetEvent(false);
        private ManualResetEvent sendEvent = new ManualResetEvent(true);
        private int timer = 3000;

        public NotifyService() : base()
        {
            Default = this;
        }

        public event Action<EndPointMessage> MessageLoad;

        public EndPointReferenceList<Instance> List { get; } = new EndPointReferenceList<Instance>();

        protected override void OnDataLoad(UdpServerEventArgs arg)
        {
            base.OnDataLoad(arg);
            //arg.Point
            var message = EndPointMessage.Read(arg.Data);
            if (message != null)
            {
                message.EndPoint = arg.Point;
                try { OnMessageLoad(message); }
                catch (Exception e) { Helper.OnException(e); }
            }
        }

        public override void Dispose()
        {
            Logout();
            base.Dispose();
        }

        public void Login()
        {
            StartListener();

            instance = Instance.GetByNetId(localPoint, true);

            byte[] temp = instance.EndPoint.GetBytes();
            Send(temp, null, SocketMessageType.Login);

            DBService.RowAccept += OnCommit;
            runEvent.Reset();
            new Task(SendData, TaskCreationOptions.LongRunning).Start();
        }

        public void Logout()
        {
            if (instance == null)
                return;
            runEvent.Set();
            DBService.RowAccept -= OnCommit;
            Send((byte[])null, null, SocketMessageType.Logout);
            StopListener();
            instance.Delete();
            instance.Save();
            instance = null;
        }

        public void Send(byte[] data, Instance address = null, SocketMessageType type = SocketMessageType.Data)
        {
            var buffer = EndPointMessage.Write(new EndPointMessage()
            {
                Sender = instance.Id.ToString(),
                Type = type,
                Data = data,
                EndPoint = localPoint
            });

            if (type == SocketMessageType.Login)
            {
                List.Clear();
                User.DBTable.Load("", DBLoadParam.Synchronize);
                foreach (Instance item in Instance.DBTable)
                {
                    if (item.Active.Value && item.EndPoint != null && item.EndPoint != localPoint && (address == null || item == address))
                    {
                        Send(buffer, item.EndPoint);
                    }
                }
            }
            else
            {
                foreach (var item in List)
                {
                    if ((address == null || item.Reference == address) &&
                        item.EndPoint != null && item.EndPoint != localPoint)
                        Send(buffer, item.EndPoint);
                }
            }
        }

        private void OnCommit(DBItemEventArgs arg)
        {
            var log = arg.Item;

            if (!(log is UserLog) && !(log is DBLogItem) && log.Table.Type == DBTableType.Table && log.Table.IsLoging)
            {
                var type = DBLogType.None;
                if ((arg.State & DBUpdateState.Delete) == DBUpdateState.Delete)
                    type = DBLogType.Delete;
                else if ((arg.State & DBUpdateState.Update) == DBUpdateState.Update)
                    type = DBLogType.Update;
                else if ((arg.State & DBUpdateState.Insert) == DBUpdateState.Insert)
                    type = DBLogType.Insert;
                if (type != DBLogType.None)
                {
                    buffer.Add(new MessageItem() { Table = log.Table, Id = log.PrimaryId, Type = type });
                }
            }
        }

        private void OnMessageLoad(EndPointMessage message)
        {
            var sender = Instance.DBTable.LoadById(message.Sender);
            if (sender == null)
                return;

            var address = List[message.EndPoint];
            if (address == null)
            {
                address = new EndPointReference<Instance>() { Reference = sender, EndPoint = message.EndPoint };
                List.Add(address);
            }
            address.Count++;
            address.Length += message.Lenght;
            switch (message.Type)
            {
                case (SocketMessageType.Hello):
                    sender.Active = true;
                    break;
                case (SocketMessageType.Login):
                    sender.Active = true;
                    Send(localPoint.GetBytes(), sender, SocketMessageType.Hello);
                    break;
                case (SocketMessageType.Logout):
                    sender.Active = false;
                    List.Remove(address);
                    break;
                case (SocketMessageType.Data):
                    CheckData(message.Data);
                    break;
            }
            MessageLoad?.Invoke(message);
        }

        private void SendData()
        {
            while (!runEvent.WaitOne(timer))
            {
                try
                {
                    var list = new MessageItem[buffer.Count > 200 ? 200 : buffer.Count];
                    if (list.Length == 0)
                        return;

                    for (int i = 0; i < list.Length; i++)
                    {
                        if (buffer.TryTake(out var item))
                        {
                            list[i] = item;
                        }
                    }

                    Array.Sort(list, (x, y) =>
                    {
                        var res = x.Table.CompareTo(y.Table);
                        res = res != 0 ? res : ListHelper.Compare(x.Id, y.Id, null, false);
                        return res != 0 ? res : x.Type.CompareTo(y.Type);
                    });
                    var stream = new MemoryStream();
                    using (var writer = new BinaryWriter(stream))
                    {
                        DBTable table = null;
                        object id = null;
                        foreach (var log in list)
                        {
                            if (log.Table != table)
                            {
                                id = null;
                                table = log.Table;
                                writer.Write(1);
                                writer.Write(table.Name);
                            }
                            if (!log.Id.Equals(id))
                            {
                                id = log.Id;
                                writer.Write(2);
                                writer.Write((int)log.Type);
                                Helper.WriteBinary(writer, log.Id, true);
                            }
                        }
                        writer.Flush();
                        Send(stream.ToArray());
                    }
                }
                catch (Exception e)
                {
                    Helper.OnException(e);
                }
            }
        }

        public static void CheckData(byte[] buffer)
        {
            lock (loadLock)
            {
                using (var transaction = new DBTransaction(loadLock, DBService.DefaultSchema.Connection, true))
                {
                    var stream = new MemoryStream(buffer);
                    using (var reader = new BinaryReader(stream))
                    {
                        while (reader.Read() == 1)
                        {
                            var tableName = reader.ReadString();
                            DBTable table = DBService.ParseTable(tableName);
                            while (reader.Read() == 2)
                            {
                                var type = (DBLogType)reader.ReadInt32();
                                var id = Helper.ReadBinary(reader);
                                if (type == DBLogType.Insert)
                                {
                                    table.LoadItemById(id, DBLoadParam.Load);
                                }
                                else if (type == DBLogType.Update)
                                {
                                    var item = table.LoadItemById(id, DBLoadParam.None);
                                    if (item != null)
                                        table.ReloadItem(id);
                                }
                                else if (type == DBLogType.Delete)
                                {
                                    var item = table.LoadItemById(id, DBLoadParam.None);
                                    if (item != null)
                                        item.Table.Remove(item);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
