using System;

namespace DataWF
{
    public class SocketConnectionArgs : EventArgs
    {
        public static readonly SocketConnectionArgs Default = new(null);

        public SocketConnectionArgs(ISocketConnection connection)
        {
            Connection = connection;
        }

        public ISocketConnection Connection { get; }
    }
}
