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
using System.Linq;

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

        class MessageItem
        {
            public DBTable Table;
            public DBLogType Type;
            public object Id;
        }

        public static NotifyService Default;

        private static object loadLock = new object();

        private Instance instance;
        private ConcurrentBag<MessageItem> buffer = new ConcurrentBag<MessageItem>();
        private ManualResetEvent runEvent = new ManualResetEvent(false);
        private int timer = 3000;
        private IPEndPoint endPoint;

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
                Instance.DBTable.Load();

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

                    var list = new MessageItem[buffer.Count > 200 ? 200 : buffer.Count];

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
                                writer.Write((char)1);
                                writer.Write(table.Name);
                            }
                            if (!log.Id.Equals(id))
                            {
                                id = log.Id;
                                writer.Write((char)2);
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
