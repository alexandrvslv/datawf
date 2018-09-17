using System;

namespace DataWF.Common
{
    public class WebNotifyClientEventArgs : EventArgs
    {
        public WebNotifyClientEventArgs(string message)
        {
            this.Message = message;
        }

        public string Message { get; }
    }
}
