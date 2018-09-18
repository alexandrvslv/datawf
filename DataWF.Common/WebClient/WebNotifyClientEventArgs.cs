using System;

namespace DataWF.Common
{
    public class WebNotifyClientEventArgs : EventArgs
    {
        public WebNotifyClientEventArgs(byte[] message)
        {
            this.Message = message;
        }

        public byte[] Message { get; }
    }
}
