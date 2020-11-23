using System;
using System.Net;

namespace DataWF.Common
{
    public class UdpServerEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
        public IPEndPoint Point { get; set; }
        public int Length { get; set; }
        public ArraySegment<byte> Data { get; set; }
        public object Tag { get; set; }
    }
}
