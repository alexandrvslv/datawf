using System;

namespace DataWF.Common
{
    public class ExceptionEventArgs : EventArgs
    {
        private readonly Exception exception;

        public ExceptionEventArgs(Exception exeption)
        {
            this.exception = exeption;
        }

        public Exception Exception
        {
            get { return exception; }
        }
    }

}

