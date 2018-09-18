using DataWF.Common;
using DataWF.Data;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Module.Common
{

    public class NotifyService : UdpServer
    {
        public static void Intergate(Action<EndPointMessage> onLoad)
        {
            var service = new NotifyService();
            service.MessageLoad += onLoad;
            service.Login();
        }

        public static NotifyService Default;

        private static object loadLock = new object();

        private Instance instance;
        private ConcurrentBag<NotifyMessageItem> buffer = new ConcurrentBag<NotifyMessageItem>();
        private ManualResetEvent runEvent = new ManualResetEvent(false);
        private int timer = 3000;
        private IPEndPoint endPoint;

        public event EventHandler<NotifyEventArgs> SendChanges;

        public NotifyService() : base()
        {
            Default = this;
        }

        public event Action<EndPointMessage> MessageLoad;

        protected override void OnDataLoad(UdpServerEventArgs arg)
        {
            base.OnDataLoad(arg);
            //arg.Point
            var message = EndPointMessage.Read(arg.Data);
            if (message != null)
            {
                message.RecivedEndPoint = arg.Point;
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
            endPoint = new IPEndPoint(EndPointHelper.GetInterNetworkIPs().First(), ListenerEndPoint.Port);
            instance = Instance.GetByNetId(endPoint, true);

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
                SenderName = instance.Id.ToString(),
                SenderEndPoint = endPoint,
                Type = type,
                Data = data
            });

            if (type == SocketMessageType.Login)
            {
                Instance.DBTable.Load().LastOrDefault();
            }

            foreach (Instance item in Instance.DBTable)
            {
                if (item.Active.Value && item.EndPoint != null && !item.EndPoint.Equals(endPoint) && (address == null || item == address))
                {
                    Send(buffer, item.EndPoint);
                }
            }
        }

        private void OnCommit(DBItemEventArgs arg)
        {
            var item = arg.Item;

            if (!(item is UserLog) && !(item is DBLogItem) && item.Table.Type == DBTableType.Table && item.Table.IsLoging)
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
                    buffer.Add(new NotifyMessageItem()
                    {
                        Item = item,
                        Type = type,
                        UserId = User.CurrentUser?.Id ?? 0
                    });
                }
            }
        }

        private void OnMessageLoad(EndPointMessage message)
        {
            var sender = Instance.DBTable.LoadById(message.SenderName);
            if (sender == null)
                return;
            sender.Count++;
            sender.Length += message.Lenght;

            switch (message.Type)
            {
                case (SocketMessageType.Hello):
                    sender.Active = true;
                    break;
                case (SocketMessageType.Login):
                    sender.Active = true;
                    Send(endPoint.GetBytes(), sender, SocketMessageType.Hello);
                    break;
                case (SocketMessageType.Logout):
                    sender.Detach();
                    break;
                case (SocketMessageType.Data):
                    LoadData(message.Data);
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
                    if (buffer.Count == 0)
                        continue;

                    var list = new NotifyMessageItem[buffer.Count > 200 ? 200 : buffer.Count];

                    for (int i = 0; i < list.Length; i++)
                    {
                        if (buffer.TryTake(out var item))
                        {
                            list[i] = item;
                        }
                    }

                    Array.Sort(list, (x, y) =>
                    {
                        var res = x.Item.Table.CompareTo(y.Item.Table);
                        res = res != 0 ? res : string.Compare(x.Item.GetType().Name, y.Item.GetType().Name, StringComparison.Ordinal);
                        res = res != 0 ? res : ListHelper.Compare(x.Item.PrimaryId, y.Item.PrimaryId, null, false);
                        return res != 0 ? res : x.Type.CompareTo(y.Type);
                    });

                    var stream = new MemoryStream();
                    using (var writer = new BinaryWriter(stream))
                    {
                        DBTable table = null;
                        object id = null;
                        foreach (var log in list)
                        {
                            if (log.Item.Table != table)
                            {
                                id = null;
                                table = log.Item.Table;
                                writer.Write((char)1);
                                writer.Write(table.Name);
                            }
                            if (!log.Item.PrimaryId.Equals(id))
                            {
                                id = log.Item.PrimaryId;
                                writer.Write((char)2);
                                writer.Write((int)log.Type);
                                writer.Write((int)log.UserId);
                                Helper.WriteBinary(writer, id, true);
                            }
                        }
                        writer.Flush();
                        Send(stream.ToArray());
                    }
                    OnSendChanges(list);
                }
                catch (Exception e)
                {
                    Helper.OnException(e);
                }
            }
        }

        protected virtual void OnSendChanges(NotifyMessageItem[] list)
        {
            SendChanges?.Invoke(this, new NotifyEventArgs(list));
        }

        public static void LoadData(byte[] buffer)
        {
            lock (loadLock)
            {
                using (var transaction = new DBTransaction(loadLock, DBService.DefaultSchema.Connection, true))
                {
                    var stream = new MemoryStream(buffer);
                    using (var reader = new BinaryReader(stream))
                    {
                        while (reader.PeekChar() == 1)
                        {
                            reader.ReadChar();
                            var tableName = reader.ReadString();
                            DBTable table = DBService.ParseTable(tableName);
                            while (reader.PeekChar() == 2)
                            {
                                reader.ReadChar();
                                var type = (DBLogType)reader.ReadInt32();
                                var user = reader.ReadInt32();
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

    public class NotifyMessageItem
    {
        public DBItem Item;
        public DBLogType Type;
        public int UserId;
    }

    public class NotifyEventArgs : EventArgs
    {
        public NotifyEventArgs(NotifyMessageItem[] data)
        {
            Data = data;
        }

        public NotifyMessageItem[] Data { get; }
    }
}
