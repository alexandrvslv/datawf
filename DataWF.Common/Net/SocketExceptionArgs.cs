using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Common
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
