using System;
using System.Text;

namespace DataWF.Common
{
    public class WebNotifyClientEventArgs : EventArgs
    {
        public WebNotifyClientEventArgs(byte[] message)
        {
            Message = message;
            if (message != null && message.Length > 0)
            {
                var messageText = Encoding.UTF8.GetString(message);
                MessageText = messageText.Trim(new char[] { '\uFEFF', '\u200B' });
            }
        }

        public WebNotifyClientEventArgs(string messageText)
        {
            MessageText = messageText.Trim(new char[] { '\uFEFF', '\u200B' });
        }

        public string MessageText { get; }

        public byte[] Message { get; }
    }
}
