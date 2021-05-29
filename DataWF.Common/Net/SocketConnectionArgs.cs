using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataWF.Common
{
    public class SocketConnectionArgs : EventArgs
    {
        public static readonly SocketConnectionArgs Default = new SocketConnectionArgs(null);

        public SocketConnectionArgs(ISocketConnection connection)
        {
            Connection = connection;
        }

        public ISocketConnection Connection { get; }
    }
}
