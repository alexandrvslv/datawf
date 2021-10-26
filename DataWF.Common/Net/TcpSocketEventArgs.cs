using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataWF.Common
{
    public class TcpSocketEventArgs : EventArgs
    {
        public TcpSocket Client { get; set; }
    }
}
