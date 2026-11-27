using System;

namespace DataWF
{
    public class ExceptionEventArgs(Exception exception) : EventArgs
    {
        public Exception Exception => exception;
    }

}

