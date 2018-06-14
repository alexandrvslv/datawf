﻿using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class UdpServerEventArgs : EventArgs
    {
        public IPEndPoint Point { get; set; }
        public int Length { get; set; }
        public byte[] Data { get; set; }
    }

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
        protected IPEndPoint localPoint;
        private ManualResetEvent receiveEvent = new ManualResetEvent(false);

        public event EventHandler<ExceptionEventArgs> DataException;
        public event EventHandler<UdpServerEventArgs> DataLoad;
        public event EventHandler<UdpServerEventArgs> DataSend;

        public UdpServer()
        {
        }

        public bool OnLine
        {
            get { return online; }
        }

        public IPEndPoint LocalPoint
        {
            get { return localPoint; }
            set { localPoint = value; }
        }

        public UdpClient Client
        {
            get { return listener; }
        }

        public void StartListener()
        {
            if (localPoint == null)
                localPoint = new IPEndPoint(IPAddress.Any, GetUdpPort());
            listener = new UdpClient();
            listener.Client.Bind(localPoint);
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
                    receiveEvent.WaitOne();
                }
            }, TaskCreationOptions.LongRunning).Start();
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                receiveEvent.Set();
                var point = new IPEndPoint(IPAddress.Any, localPoint.Port);
                var arg = result.AsyncState as UdpServerEventArgs;
                arg.Data = listener.EndReceive(result, ref point);
                arg.Length = arg.Data.Length;
                arg.Point = point;
                OnDataLoad(arg);
            }
            catch (Exception ex)
            {
                //if (ex is SocketException && ((SocketException)ex).ErrorCode == 10060)
                OnDataException(new ExceptionEventArgs(ex));
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

        public void Send(byte[] data, IPEndPoint address)
        {
            if (address != null && data != null)
            {
                var param = new UdpServerEventArgs { Data = data, Point = address };
                sender.BeginSend(data, data.Length, address, SendCallback, param);
            }
        }

        private void SendCallback(IAsyncResult result)
        {
            var arg = result.AsyncState as UdpServerEventArgs;
            try
            {
                arg.Length = sender.EndSend(result);
                OnDataSend(arg);
            }
            catch (Exception ex)
            {
                OnDataException(new ExceptionEventArgs(ex));
            }
        }

        protected virtual void OnDataSend(UdpServerEventArgs arg)
        {
            DataSend?.Invoke(this, arg);
            NetStat.Set("Data Send", 1, arg.Length);

        }

        protected virtual void OnDataLoad(UdpServerEventArgs arg)
        {
            DataLoad?.Invoke(this, arg);
            NetStat.Set("Data Receive", 1, arg.Length);
        }

        protected virtual void OnDataException(ExceptionEventArgs ex)
        {
            DataException?.Invoke(this, ex);
            Helper.OnException(ex.Exception);
        }

        public virtual void Dispose()
        {
            if (online)
                StopListener();
            listener?.Dispose();
            sender?.Dispose();
            receiveEvent?.Dispose();
        }
    }
}
