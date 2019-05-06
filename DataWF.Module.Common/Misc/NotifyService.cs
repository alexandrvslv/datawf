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
        public static void Intergate(Action<EndPointMessage> onLoad, User user)
        {
            var service = new NotifyService();
            service.MessageLoad += onLoad;
            service.Login(user);
        }

        public static NotifyService Default;

        private static readonly object loadLock = new object();

        private Instance instance;
        private ConcurrentBag<NotifyMessageItem> buffer = new ConcurrentBag<NotifyMessageItem>();
        private ManualResetEvent runEvent = new ManualResetEvent(false);
        private const int timer = 3000;

        public User User { get; private set; }

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

        public async void Login(User user)
        {
            StartListener();
            User = user;
            endPoint = new IPEndPoint(EndPointHelper.GetInterNetworkIPs().First(), ListenerEndPoint.Port);
            instance = await Instance.GetByNetId(endPoint, user, true);

            byte[] temp = instance.EndPoint.GetBytes();
            Send(temp, null, SocketMessageType.Login);

            DBService.RowAccept = OnCommit;
            runEvent.Reset();
            new Task(SendData, TaskCreationOptions.LongRunning).Start();
        }

        public async void Logout()
        {
            if (instance == null)
                return;
            runEvent.Set();
            DBService.RowAccept = null;
            Send((byte[])null, null, SocketMessageType.Logout);
            StopListener();
            instance.Delete();
            await instance.Save(User);
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
                Instance.DBTable.Load();
            }

            foreach (Instance item in Instance.DBTable)
            {
                if (address == null || item == address)
                {
                    if (item.Active.Value && item.EndPoint != null && !item.EndPoint.Equals(endPoint) && !IPAddress.Loopback.Equals(item.EndPoint.Address))
                    {
                        Send(buffer, item.EndPoint);
                    }
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
                else if ((arg.State & DBUpdateState.Insert) == DBUpdateState.Insert)
                    type = DBLogType.Insert;
                else
                    type = DBLogType.Update;
                buffer.Add(new NotifyMessageItem()
                {
                    Table = item.Table,
                    ItemId = item.PrimaryId,
                    Type = type,
                    UserId = arg.User?.Id ?? 0
                });
            }
        }

        protected virtual void OnMessageLoad(EndPointMessage message)
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
                        var res = x.Table.CompareTo(y.Table);
                        //res = res != 0 ? res : string.Compare(x.Item.GetType().Name, y.Item.GetType().Name, StringComparison.Ordinal);
                        res = res != 0 ? res : ListHelper.Compare(x.ItemId, y.ItemId, null, false);
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
                                writer.Write((char)1);
                                writer.Write(table.Name);
                            }
                            if (!log.ItemId.Equals(id))
                            {
                                id = log.ItemId;
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
                using (var transaction = new DBTransaction(DBService.Schems.DefaultSchema.Connection, null, true))
                {
                    var stream = new MemoryStream(buffer);
                    using (var reader = new BinaryReader(stream))
                    {
                        while (reader.PeekChar() == 1)
                        {
                            reader.ReadChar();
                            var tableName = reader.ReadString();
                            DBTable table = DBService.Schems.ParseTable(tableName);
                            if (table == null)
                            {
                                continue;
                            }

                            while (reader.PeekChar() == 2)
                            {
                                reader.ReadChar();
                                var type = (DBLogType)reader.ReadInt32();
                                var user = reader.ReadInt32();
                                var id = Helper.ReadBinary(reader);
                                if (type == DBLogType.Insert)
                                {
                                    table.LoadItemById(id, DBLoadParam.Load, null, transaction);
                                }
                                else if (type == DBLogType.Update)
                                {
                                    var item = table.LoadItemById(id, DBLoadParam.None);
                                    if (item != null)
                                    {
                                        table.ReloadItem(id, DBLoadParam.Load, transaction);
                                    }
                                }
                                else if (type == DBLogType.Delete)
                                {
                                    var item = table.LoadItemById(id, DBLoadParam.None);
                                    if (item != null)
                                    {
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

    public class NotifyMessageItem
    {
        public DBTable Table;
        public object ItemId;
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
