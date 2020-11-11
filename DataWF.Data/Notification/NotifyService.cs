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
                        map[itemType] = new NotifyDBTable { Type = itemType, Table = item.Value.Table };
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

        public static void Intergate(Action<EndPointMessage> onLoad)
        {
            var service = new NotifyService();
            service.MessageLoad += onLoad;
            service.Start();
        }

        public static NotifyService Default;

        private static readonly object loadLock = new object();

        private IInstance instance;
        private readonly ConcurrentQueue<NotifyDBItem> buffer = new ConcurrentQueue<NotifyDBItem>();
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
            Send(temp, null, SocketMessageType.Login, true);

            DBService.AddRowAccept(OnAccept);
            runEvent.Reset();
            new Task(SendChangesRunner, TaskCreationOptions.LongRunning).Start();
        }

        public async void Logout()
        {
            if (instance == null)
                return;
            runEvent.Set();
            DBService.RemoveRowAccept(OnAccept);
            Send(null, null, SocketMessageType.Logout);
            StopListener();
            instance.Delete();
            await instance.Save(User);
            instance = null;
        }

        public void Send(List<NotifyDBTable> items, IInstance address = null)
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
                    Send(buffer, item.EndPoint, item);
                }
            }
            SendChanges?.Invoke(this, new NotifyEventArgs(items));
        }

        public void Send(byte[] data, IInstance address = null, SocketMessageType type = SocketMessageType.Data, bool checkState = false)
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
                if (CheckAddress(item, address, checkState))
                {
                    Send(buffer, item.EndPoint, item);
                }
            }
        }

        protected override void OnDataSend(UdpServerEventArgs arg)
        {
            base.OnDataSend(arg);
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
                    Diff = type,
                    User = arg.User?.Id ?? 0
                });
            }
            return default;
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
            sender.ReceiveCount++;
            sender.ReceiveLength += message.Lenght;

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
                    List<NotifyDBTable> list = Dequeu(buffer);

                    Send(list);
                }
                catch (Exception e)
                {
                    Helper.OnException(e);
                }
            }
        }

        private static byte[] Serialize(List<NotifyDBTable> list)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryInvokerWriter(stream))
            {
                writer.WriteObjectBegin();
                writer.WriteArrayBegin();

                foreach (var item in list)
                {
                    writer.WriteArrayEntry();
                }

                writer.WriteArrayEnd();
                writer.WriteObjectEnd();
                writer.Flush();
                return stream.ToArray();
            }
        }

        public static void Deserialize(byte[] buffer)
        {
            lock (loadLock)
            {
                using (var transaction = new DBTransaction(DBService.Schems.DefaultSchema.Connection, null, true))
                {
                    using (var stream = new MemoryStream(buffer))
                    using (var reader = new BinaryInvokerReader(stream))
                    {
                        reader.ReadToken();
                        reader.ReadToken();
                        while (reader.ReadToken() == BinaryToken.ArrayEntry)
                        {
                            var typeTable = new NotifyDBTable();
                            typeTable.Deserialize(reader);
                            if (typeTable.Type == null)
                            {
                                continue;
                            }
                            foreach (var item in typeTable.Items)
                            {
                                switch (item.Diff)
                                {
                                    case DBLogType.Insert:
                                        typeTable.Table.LoadItemById(item.Id, DBLoadParam.Load, null, transaction);
                                        break;
                                    case DBLogType.Update:
                                        {
                                            var record = typeTable.Table.LoadItemById(item.Id, DBLoadParam.None);
                                            if (record != null)
                                            {
                                                typeTable.Table.ReloadItem(item.Id, DBLoadParam.Load, transaction);
                                            }
                                            break;
                                        }
                                    case DBLogType.Delete:
                                        {
                                            var record = typeTable.Table.LoadItemById(item.Id, DBLoadParam.None);
                                            if (item != null)
                                            {
                                                typeTable.Table.Remove(record);
                                            }
                                            break;
                                        }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public class NotifyDBTable : IByteSerializable, IComparable<NotifyDBTable>
    {
        private DBTable table;

        [XmlIgnore, JsonIgnore]
        public DBTable Table
        {
            get => table ?? (table = DBTable.GetTable(Type));
            set => table = value;
        }

        public Type Type { get; set; }

        public List<NotifyDBItem> Items { get; set; } = new List<NotifyDBItem>();

        public int CompareTo(NotifyDBTable other)
        {
            return Table.CompareTo(other.Table);
        }

        public void Deserialize(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            using (var reader = new BinaryReader(stream))
            {
                Deserialize(reader);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            using (var invokerReader = new BinaryInvokerReader(reader))
            {
                Deserialize(invokerReader);
            }
        }

        public void Deserialize(BinaryInvokerReader invokerReader)
        {
            invokerReader.ReadToken();
            invokerReader.ReadToken();
            var name = invokerReader.ReadString();
            Type = TypeHelper.ParseType(name);
            invokerReader.ReadToken();
            invokerReader.ReadToken();
            while (invokerReader.ReadToken() == BinaryToken.ArrayEntry)
            {
                var item = new NotifyDBItem();
                item.Deserialize(invokerReader.Reader);
                Items.Add(item);
            }
        }

        public byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Serialize(writer);
                return stream.ToArray();
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            using (var invokerWriter = new BinaryInvokerWriter(writer))
            {
                Serialize(invokerWriter);
            }
        }

        public void Serialize(BinaryInvokerWriter invokerWriter)
        {
            invokerWriter.WriteObjectBegin();
            invokerWriter.WriteObjectEntry();
            invokerWriter.WriteString(TypeHelper.FormatBinary(Type), false);
            invokerWriter.WriteObjectEntry();
            invokerWriter.WriteArrayBegin();
            foreach (var item in Items)
            {
                invokerWriter.WriteArrayEntry();
                item.Serialize(invokerWriter.Writer);
            }
            invokerWriter.WriteArrayEnd();
            invokerWriter.WriteObjectEnd();
        }
    }

    public class NotifyDBItem : IByteSerializable, IComparable<NotifyDBItem>
    {
        public DBLogType Diff { get; set; }
        public int User { get; set; }
        public object Id { get; set; }

        [XmlIgnore, JsonIgnore]
        public DBItem Value { get; set; }

        public int CompareTo(NotifyDBItem other)
        {
            var res = ListHelper.Compare(Id, other.Id, null);
            return res != 0 ? res : Diff.CompareTo(Diff);
        }

        public void Deserialize(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            using (var reader = new BinaryReader(stream))
            {
                Deserialize(reader);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            Diff = (DBLogType)reader.ReadByte();
            User = reader.ReadInt32();
            Id = Helper.ReadBinary(reader);
        }

        public byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                Serialize(writer);
                return stream.ToArray();
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Diff);
            writer.Write((int)User);
            Helper.WriteBinary(writer, Id, true);
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
