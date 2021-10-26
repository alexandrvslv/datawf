using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Messanger
{
    public abstract class MessageDetail : DBItem
    {
        private Message message;

        [Browsable(false)]
        [DataMember, Column("message_id")]
        public long? MessageId
        {
            get { return GetProperty<long?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(MessageId))]
        public Message Message
        {
            get { return GetPropertyReference(ref message); }
            set { SetPropertyReference(message = value); }
        }
    }

}