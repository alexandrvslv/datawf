using System;

namespace DataWF.WebService.Common
{
    public class WebNotifyEventArgs : EventArgs
    {
        public WebNotifyEventArgs(WebNotifyConnection client, string message = null)
        {
            Client = client;
            Message = message;
        }

        public WebNotifyConnection Client { get; }
        public string Message { get; }
    }
}
