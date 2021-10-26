using System;
using System.IO;
using System.Net;

namespace DataWF.Common
{
    public class TcpServerEventArgs : EventArgs
    {
        public static int BufferSize = 2048;

        public byte[] Buffer { get; set; }
        public TcpSocket Client { get; set; }
        public Stream Stream { get; set; }
        public object Tag { get; set; }

        public IPEndPoint Point { get { return Client.Socket == null ? null : Client.Socket.RemoteEndPoint as IPEndPoint; } }
        public long Length { get { return Stream == null ? 0L : Stream.Length; } }

        public TcpServerEventArgs()
        {
            Buffer = new byte[BufferSize];
        }

        public TcpServerEventArgs Clone(bool stream = true)
        {
            var clone = new TcpServerEventArgs { Client = this.Client };
            if (stream)
                clone.Stream = new MemoryStream();
            return clone;
        }
    }



}
