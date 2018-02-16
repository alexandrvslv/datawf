using System;
using System.ComponentModel;
using System.Net;

namespace DataWF.Common
{
    public class SocketAddress
    {
        public object Tag { get; set; }
        public IPEndPoint Point { get; set; }
        public uint Count { get; set; }
        [Browsable(false)]
        public ulong Length { get; set; }
        public decimal Size { get { return Math.Round(Length / 1024M, 2); } }

        public override string ToString()
        {
            return string.Format("{0}-{1}", Tag, Point);
        }
    }

    public class SocketAddressList : SelectableList<SocketAddress>
    {

        public SocketAddress this[string point]
        {
            get { return Find("Point.ToString", CompareType.Equal, point); }
        }

        public SocketAddress this[IPEndPoint point]
        {
            get { return Find("Point", CompareType.Equal, point); }
        }
    }

}
