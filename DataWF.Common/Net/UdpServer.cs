﻿using System;
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
        public static int GetUdpPort()
        {
            var prop = IPGlobalProperties.GetIPGlobalProperties();
            var active = prop.GetActiveUdpListeners();
            int myport = 49152;
            for (; myport < 65535; myport++)
            {
                bool alreadyinuse = false;
                foreach (var p in active)
                    if (p.Port == myport)
                    {
                        alreadyinuse = true;
                        break;
                    }
                if (!alreadyinuse)
                {
                    break;
                }
            }
            return myport;
        }

        protected bool online;
        protected UdpClient listener;
        protected UdpClient sender;
        private IPEndPoint listenerEndPoint;
        private readonly ManualResetEventSlim receiveEvent = new ManualResetEventSlim(false);

        public event EventHandler<UdpServerEventArgs> DataException;
        public event EventHandler<UdpServerEventArgs> DataLoad;
        public event EventHandler<UdpServerEventArgs> DataSend;

        public UdpServer()
        {
        }

        public bool OnLine
        {
            get { return online; }
        }

        public IPEndPoint ListenerEndPoint
        {
            get { return listenerEndPoint; }
            set { listenerEndPoint = value; }
        }

        public UdpClient Client
        {
            get { return listener; }
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
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            if (!online)
                return;
            var arg = result.AsyncState as UdpServerEventArgs;
            try
            {
                receiveEvent.Set();
                var point = new IPEndPoint(IPAddress.Any, listenerEndPoint.Port);

                arg.Data = listener.EndReceive(result, ref point);
                arg.Length = arg.Data.Length;
                arg.Point = point;
                OnDataLoad(arg);
            }
            catch (Exception ex)
            {
                arg.Exception = ex;
                //if (ex is SocketException && ((SocketException)ex).ErrorCode == 10060)
                OnDataException(arg);
            }
        }

        public void Send(byte[] data, string address)
        {
            Send(data, TcpServer.ParseEndPoint(address));
        }

        public void Send(string data, string address)
        {
            Send(Encoding.UTF8.GetBytes(data), TcpServer.ParseEndPoint(address));
        }

        public void Send(byte[] data, IPEndPoint address, object tag = null)
        {
            if (address != null && data != null)
            {
                var param = new UdpServerEventArgs { Data = data, Point = address, Tag = tag };
                sender.BeginSend(data, data.Length, address, SendCallback, param);
            }
        }

        private void SendCallback(IAsyncResult result)
        {
            var arg = result.AsyncState as UdpServerEventArgs;
            try
            {
                if (sender == null)
                    return;
                arg.Length = sender.EndSend(result);
                OnDataSend(arg);
            }
            catch (Exception ex)
            {
                arg.Exception = ex;
                OnDataException(arg);
            }
        }

        protected virtual void OnDataSend(UdpServerEventArgs arg)
        {
            NetStat.Set("Data Send", 1, arg.Length);
            DataSend?.Invoke(this, arg);

        }

        protected virtual void OnDataLoad(UdpServerEventArgs arg)
        {
            NetStat.Set("Data Receive", 1, arg.Length);
            DataLoad?.Invoke(this, arg);
        }

        protected virtual void OnDataException(UdpServerEventArgs arg)
        {
            NetStat.Set("Errors", 1, arg.Data?.Length ?? 0);
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
