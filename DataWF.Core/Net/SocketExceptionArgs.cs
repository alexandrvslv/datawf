using System;

namespace DataWF
{
    public class SocketExceptionArgs : EventArgs
    {
        public SocketExceptionArgs(SocketConnectionArgs argument, Exception exception)
        {
            Arguments = argument;
            Exception = exception;
        }

        public SocketConnectionArgs Arguments { get; }

        public Exception Exception { get; }

    }
}
