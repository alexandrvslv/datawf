using System;
using System.Text;

namespace DataWF.Common
{
    public class WebNotifyClientEventArgs : EventArgs
    {
        private string messageText;

        public WebNotifyClientEventArgs(byte[] message)
        {
            Message = message;
        }
        
        public byte[] Message { get; }

        public string MessageText
        {
            get
            {
                if (Message != null && Message.Length > 0)
                {
                    messageText = Encoding.UTF8.GetString(Message);
                    messageText = messageText.Trim(new char[] { '\uFEFF', '\u200B' });
                }
                return messageText;
            }
        }

    }
}
