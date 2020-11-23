using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{

    public class UdpServer : IDisposable
    {
        protected bool online;
        protected UdpClient listener;
        protected UdpClient sender;
        private IPEndPoint listenerEndPoint;
        private BinarySerializer serializer;
        private readonly ManualResetEventSlim receiveEvent = new ManualResetEventSlim(false);

        public event EventHandler<UdpServerEventArgs> DataException;
        public event EventHandler<UdpServerEventArgs> DataLoad;
        public event EventHandler<UdpServerEventArgs> DataSend;

        public UdpServer()
        {
            serializer = new BinarySerializer();
        }

        public bool OnLine
        {
            get => online;
        }

        public IPEndPoint ListenerEndPoint
        {
            get => listenerEndPoint;
            set => listenerEndPoint = value;
        }

        public UdpClient Client
        {
            get => listener;
        }

        public void StartListener()
        {
            if (listenerEndPoint == null)
                listenerEndPoint = new IPEndPoint(IPAddress.Any, 0);
            listener = new UdpClient();
            listener.Client.Bind(listenerEndPoint);
            listenerEndPoint = (IPEndPoint)listener.Client.LocalEndPoint;
            //this.listener.Client.SendTimeout = 5000;
            //this.listener.Client.ReceiveTimeout = 5000;
            sender = new UdpClient();
            online = true;
            WaiteData();
        }

        public void StopListener()
        {
            online = false;
            listener.Close();
            sender.Close();
        }

        private void WaiteData()
        {
            new Task(() =>
            {
                while (online)
                {
                    receiveEvent.Reset();
                    listener.BeginReceive(ReceiveCallback, new UdpServerEventArgs());
                    receiveEvent.Wait();
                }
            }, TaskCreationOptions.LongRunning).Start();

            void ReceiveCallback(IAsyncResult result)
            {
                if (!online)
                    return;
                var arg = result.AsyncState as UdpServerEventArgs;
                try
                {
                    receiveEvent.Set();
                    var point = new IPEndPoint(IPAddress.Any, listenerEndPoint.Port);
                    var buffer = listener.EndReceive(result, ref point);
                    //TODO Compression\Decompression
                    arg.Data = new ArraySegment<byte>(buffer);
                    arg.Length = arg.Data.Count;
                    arg.Point = point;
                    _ = OnDataLoad(arg);
                }
                catch (Exception ex)
                {
                    arg.Exception = ex;
                    //if (ex is SocketException && ((SocketException)ex).ErrorCode == 10060)
                    OnDataException(arg);
                }
            }
        }

        public ValueTask Send(byte[] data, string address)
        {
            return Send(new ArraySegment<byte>(data), SocketHelper.ParseEndPoint(address));
        }

        public ValueTask Send(string data, string address)
        {
            return Send(new ArraySegment<byte>(Encoding.UTF8.GetBytes(data)), SocketHelper.ParseEndPoint(address));
        }

        public ValueTask SendElement<T>(T element, IPEndPoint address, object tag = null)
        {
            return Send(serializer.Serialize<T>(element), address, tag);
        }

        public async ValueTask Send(ArraySegment<byte> data, IPEndPoint address, object tag = null)
        {
            if (address != null && data != null)
            {
                var arg = new UdpServerEventArgs { Data = data, Point = address, Tag = tag };
                try
                {
                    //TODO Compression\Decompression
                    arg.Length = await Task.Factory.FromAsync<int>(sender.BeginSend(arg.Data.Array, arg.Data.Count, address, null, arg), sender.EndSend);
                    await OnDataSend(arg);
                }
                catch (Exception ex)
                {
                    arg.Exception = ex;
                    OnDataException(arg);
                }
            }
        }

        protected virtual ValueTask OnDataSend(UdpServerEventArgs arg)
        {
            NetStat.Set("Data Send", 1, arg.Length);
            DataSend?.Invoke(this, arg);
            return default;
        }

        protected virtual ValueTask OnDataLoad(UdpServerEventArgs arg)
        {
            NetStat.Set("Data Receive", 1, arg.Length);
            DataLoad?.Invoke(this, arg);
            return default;
        }

        protected virtual void OnDataException(UdpServerEventArgs arg)
        {
            NetStat.Set("Errors", 1, arg.Data.Count);
            DataException?.Invoke(this, arg);

            Helper.OnException(arg.Exception);
        }

        public virtual void Dispose()
        {
            if (online)
                StopListener();
            listener?.Dispose();
            listener = null;
            sender?.Dispose();
            sender = null;
            receiveEvent?.Dispose();
        }
    }
}
