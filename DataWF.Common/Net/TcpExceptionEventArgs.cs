using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataWF.Common
{
    public class TcpExceptionEventArgs : TcpSocketEventArgs
    {
        public TcpExceptionEventArgs(EventArgs argument, Exception exception)
        {
            Arguments = argument;
            Exception = exception;
        }

        public EventArgs Arguments { get; set; }

        public Exception Exception { get; set; }

    }
}
