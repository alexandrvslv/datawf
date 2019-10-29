using DataWF.Common;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Data
{
    public class NotifyService : UdpServer
    {
        public static void Intergate(Action<EndPointMessage> onLoad)
        {
            var service = new NotifyService();
            service.MessageLoad += onLoad;
            service.Start();
        }

        public static NotifyService Default;

        private static readonly object loadLock = new object();

        private IInstance instance;
        private readonly ConcurrentQueue<NotifyMessageItem> buffer = new ConcurrentQueue<NotifyMessageItem>();
        private readonly ManualResetEventSlim runEvent = new ManualResetEventSlim(false);
        private const int timer = 2000;

        public IUserIdentity User { get; private set; }

        private IPEndPoint endPoint;

        public event EventHandler<NotifyEventArgs> SendChanges;

        public NotifyService() : base()
        {
            Default = this;
        }

        public event Action<EndPointMessage> MessageLoad;

        public override void Dispose()
        {
            Logout();
            base.Dispose();
        }

        public async void Start()
        {
            StartListener();
            endPoint = new IPEndPoint(EndPointHelper.GetInterNetworkIPs().First(), ListenerEndPoint.Port);
            instance = await Instance.GetByNetId(endPoint, true, User);

            byte[] temp = instance.EndPoint.GetBytes();
            Send(temp, null, SocketMessageType.Login);

            DBService.RowAccept = OnCommit;
            runEvent.Reset();
            new Task(SendChangesRunner, TaskCreationOptions.LongRunning).Start();
        }

        public async void Logout()
        {
            if (instance == null)
                return;
            runEvent.Set();
            DBService.RowAccept = null;
            Send(null, null, SocketMessageType.Logout);
            StopListener();
            instance.Delete();
            await instance.Save(User);
            instance = null;
        }

        public void Send(NotifyMessageItem[] items, IInstance address = null)
        {
            var buffer = (byte[])null;

            foreach (IInstance item in Instance.DBTable)
            {
                if (CheckAddress(item, address))
                {
                    if (buffer == null)
                    {
                        buffer = EndPointMessage.Write(new EndPointMessage
                        {
                            SenderName = instance.Id.ToString(),
                            SenderEndPoint = endPoint,
                            Type = SocketMessageType.Data,
                            Data = Serialize(items)
                        });
                    }
                    Send(buffer, item.EndPoint);
                }
            }
        }

        public void Send(byte[] data, IInstance address = null, SocketMessageType type = SocketMessageType.Data)
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

            foreach (IInstance item in Instance.DBTable)
            {
                if (CheckAddress(item, address))
                {
                    Send(buffer, item.EndPoint);
                }
            }
        }

        private bool CheckAddress(IInstance item, IInstance address)
        {
            return (address == null || item == address)
                && item.Active.Value
                && item.EndPoint != null
                && !item.EndPoint.Equals(endPoint)
                && !IPAddress.Loopback.Equals(item.EndPoint.Address);
        }

        private void OnCommit(DBItemEventArgs arg)
        {
            var item = arg.Item;

            if (!(item is DBLogItem) && item.Table.Type == DBTableType.Table && item.Table.IsLoging)
            {
                var type = (arg.State & DBUpdateState.Delete) == DBUpdateState.Delete ? DBLogType.Delete
                    : (arg.State & DBUpdateState.Insert) == DBUpdateState.Insert ? DBLogType.Insert
                    : DBLogType.Update;
                buffer.Enqueue(new NotifyMessageItem()
                {
                    Table = item.Table,
                    ItemId = item.PrimaryId,
                    Type = type,
                    UserId = arg.User?.Id ?? 0
                });
            }
        }

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
                    Deserialize(message.Data);
                    break;
            }
            MessageLoad?.Invoke(message);
        }

        private void SendChangesRunner()
        {
            while (!runEvent.Wait(timer))
            {
                try
                {
                    if (buffer.Count == 0)
                        continue;

                    var list = new NotifyMessageItem[buffer.Count > 200 ? 200 : buffer.Count];

                    for (int i = 0; i < list.Length; i++)
                    {
                        if (buffer.TryDequeue(out var item))
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
            Send(list);
            SendChanges?.Invoke(this, new NotifyEventArgs(list));
        }

        private static byte[] Serialize(NotifyMessageItem[] list)
        {
            using (var stream = new MemoryStream())
            {
                Serialize(list, stream);
                return stream.ToArray();
            }
        }

        private static void Serialize(NotifyMessageItem[] list, MemoryStream stream)
        {
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
            }
        }

        public static void Deserialize(byte[] buffer)
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
