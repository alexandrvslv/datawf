using DataWF.Data;
using DataWF.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Timers;
using DataWF.Module.Common;

namespace DataWF.Module.Flow
{
    public class UDPService : UdpServer
    {
        public static void Intergate(User user)
        {
            UDPService service = new UDPService();
            service.MessageLoad += ServiceLoaded;
            service.StartListener();
            service.Login(user);
        }

        private static void ServiceLoaded(SocketMessage obj)
        {
            User user = User.DBTable.LoadById(obj.Sender);
            if (user == null || user.IsCurrent)
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
            public UserLogType Type;
            public object Id;
        }
        public static UDPService Default;
        private User user;
        private SocketAddressList list = new SocketAddressList();
        private ConcurrentBag<MessageItem> buffer = new ConcurrentBag<MessageItem>();
        private ManualResetEvent delayEvent = new ManualResetEvent(false);
        private ManualResetEvent sendEvent = new ManualResetEvent(true);
        private int timer = 3000;
        public event Action<SocketMessage> MessageLoad;
        private static Queue<LoadTask> loadQueue = new Queue<LoadTask>();
        private bool run = false;

        public UDPService()
            : base()
        {
            Default = this;
        }

        public SocketAddressList List
        {
            get { return list; }
        }

        protected override void OnDataLoad(UdpServerEventArgs arg)
        {
            base.OnDataLoad(arg);
            //arg.Point
            var message = SocketMessage.Read(arg.Data);
            if (message != null)
                try { OnMessageLoad(message); }
                catch (Exception e) { Helper.OnException(e); }
        }

        public override void Dispose()
        {
            if (run)
                Logout();
            base.Dispose();
        }

        public void SaveEndPoint(string endpoint)
        {
            User.DBTable.IsLoging = false;
            user.NetworkAddress = endpoint;
            user.Save();
            User.DBTable.IsLoging = true;
            //Send(user.NetworkId, null, SocketMessageType.Hello);
        }

        public void Login(User value)
        {
            if (value == null)
                return;

            StartListener();

            user = value;

            if (user.NetworkAddress.Length > 0 && user.NetworkAddress.IndexOf(';') < 0)
                user.NetworkAddress = string.Empty;

            string endpoint = localPoint + ";";
            if (user.NetworkAddress.IndexOf(endpoint, StringComparison.OrdinalIgnoreCase) < 0)
                SaveEndPoint(user.NetworkAddress + endpoint);

            Send(user.NetworkAddress, null, SocketMessageType.Login);

            DBService.RowAccept += OnCommit;
            run = true;
            ThreadPool.QueueUserWorkItem(SendData);

        }

        public void Logout()
        {
            run = false;
            DBService.RowAccept -= OnCommit;
            SaveEndPoint(string.Empty);//user.NetworkId.Replace(localPoint.ToString() + ";", string.Empty)
            Send(string.Empty, null, SocketMessageType.Logout);
            StopListener();
        }

        public void Send(string data, User address = null, SocketMessageType type = SocketMessageType.Data)
        {
            string endpoint = localPoint.ToString();
            SocketMessage message = new SocketMessage() { Sender = this.user.PrimaryId.ToString(), Type = type, Data = data, Point = endpoint };
            byte[] buf = SocketMessage.Write(message);
            if (buf.Length > 1000)
                buf = Helper.WriteGZip(buf);

            if (type == SocketMessageType.Login)
            {
                list.Clear();
                User.DBTable.Load("", DBLoadParam.Synchronize);
                foreach (User item in User.DBTable)
                    if (item.NetworkAddress.Length > 0 && (address == null || item == address))
                    {
                        string[] split = item.NetworkAddress.Split(';');

                        foreach (string point in split)
                        {
                            if (point.Length != 0 && point != endpoint)
                                Send(buf, point);
                        }
                    }
            }
            else
            {
                foreach (SocketAddress item in list)
                {
                    if ((address == null || item.Tag == address) &&
                        item.Point != null && item.Point.ToString() != endpoint)
                        Send(buf, item.Point);
                }
            }
        }

        private void OnCommit(DBItemEventArgs arg)
        {
            var log = arg.Item;

            if (!(log is UserLog) && log.Table.Type == DBTableType.Table &&
                (log.Table == DocumentWork.DBTable || log.Table.IsLoging))
            {
                UserLogType type = UserLogType.None;
                if ((arg.State & DBUpdateState.Delete) == DBUpdateState.Delete)
                    type = UserLogType.Delete;
                else if ((arg.State & DBUpdateState.Update) == DBUpdateState.Update)
                    type = UserLogType.Update;
                else if ((arg.State & DBUpdateState.Insert) == DBUpdateState.Insert)
                    type = UserLogType.Insert;
                if (type != UserLogType.None)
                {
                    buffer.Add(new MessageItem() { Table = log.Table, Id = log.PrimaryId, Type = type });
                }
            }
        }

        private void SendData(object sender)
        {
            while (run)
            {
                try
                {
                    var list = new MessageItem[buffer.Count > 200 ? 200 : buffer.Count];
                    for (int i = 0; i < list.Length; i++)
                    {
                        MessageItem item;
                        if (buffer.TryTake(out item))
                        {
                            list[i] = item;
                        }
                    }
                    Array.Sort<MessageItem>(list, (x, y) =>
                    {
                        var res = x.Table.CompareTo(y.Table);
                        res = res != 0 ? res : ListHelper.Compare(x.Id, y.Id, null, false);
                        return res != 0 ? res : x.Type.CompareTo(y.Type);
                    });
                    var temp = new StringBuilder();
                    DBTable table = null;
                    object id = null;
                    foreach (var log in list)
                    {
                        if (log.Table != table)
                        {
                            id = null;
                            table = log.Table;
                            temp.AppendFormat("\t{0}", table.FullName);
                        }
                        if (!log.Id.Equals(id))
                        {
                            id = log.Id;
                            temp.AppendFormat(";{0}{1}", log.Type == UserLogType.Insert ? "I" :
                                                         log.Type == UserLogType.Update ? "U" :
                                                         log.Type == UserLogType.Delete ? "D" : "",
                                                         log.Id);
                        }
                    }
                    if (temp.Length > 0)
                        Send(temp.ToString());
                }
                catch (Exception e)
                {
                    Helper.OnException(e);
                }
                delayEvent.WaitOne(timer);
            }
        }

        private void OnMessageLoad(SocketMessage message)
        {
            User sender = User.DBTable.LoadById(message.Sender);
            if (sender == null)
                return;
            SocketAddress address = list[message.Point];
            if (address == null)
            {
                address = new SocketAddress() { Tag = sender, Point = TcpServer.ParseEndPoint(message.Point) };
                list.Add(address);
            }
            address.Count++;
            address.Length += message.Lenght;
            switch (message.Type)
            {
                case (SocketMessageType.Hello):
                    sender.Online = true;
                    break;
                case (SocketMessageType.Login):
                    sender.Online = true;
                    Send(user.NetworkAddress, sender, SocketMessageType.Hello);
                    break;
                case (SocketMessageType.Logout):
                    sender.Online = false;
                    list.Remove(address);
                    break;
                case (SocketMessageType.Data):
                    CheckData(message.Data);
                    break;
            }
            if (MessageLoad != null)
                MessageLoad(message);
        }

        private struct LoadTask
        {
            public DBTable Table;
            public bool Refresh;
            public object Id;
        }

        public static void LoadData()
        {
            if (loadQueue.Count > 0)
            {
                lock (loadQueue)
                {
                    using (var transaction = new DBTransaction(DBService.DefaultSchema.Connection))
                    {
                        while (loadQueue.Count > 0)
                        {
                            var task = loadQueue.Dequeue();
                            if (task.Refresh)
                                task.Table.ReloadItem(task.Id);
                            else
                                task.Table.LoadItemById(task.Id, DBLoadParam.Load);
                        }
                    }
                }
            }
        }

        public static void CheckData(string p)
        {
            lock (loadQueue)
            {
                string[] recs = p.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string rec in recs)
                {
                    string[] split = rec.Split(new char[] { ';' });
                    DBTable table = DBService.ParseTable(split[0]);
                    if (table != null && table.Schema == DBService.DefaultSchema)
                    {
                        for (int i = 1; i < split.Length; i++)
                        {
                            var type = split[i][0];
                            var id = table.PrimaryKey.ParseValue(split[i].Substring(1));
                            if (type == 'I')
                            {
                                loadQueue.Enqueue(new LoadTask() { Table = table, Refresh = false, Id = id });
                            }
                            else if (type == 'U')
                            {
                                DBItem row = table.LoadItemById(id, DBLoadParam.None);
                                if (row != null)
                                    loadQueue.Enqueue(new LoadTask() { Table = table, Refresh = true, Id = id });
                            }
                            else if (type == 'D')
                            {
                                DBItem row = table.LoadItemById(id, DBLoadParam.None);
                                if (row != null)
                                    row.Table.Remove(row);
                            }
                        }
                    }
                }
            }
            LoadData();
        }

    }
}
