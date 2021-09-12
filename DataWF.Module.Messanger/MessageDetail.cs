using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Messanger
{
    [AbstractTable, InvokerGenerator]
    public abstract partial class MessageDetail : DBItem
    {
        private Message message;

        [Browsable(false)]
        [DataMember, Column("message_id")]
        public long? MessageId
        {
            get => GetValue(Table.MessageIdKey);
            set => SetValue(value, Table.MessageIdKey);
        }

        [Reference(nameof(MessageId))]
        public Message Message
        {
            get => GetReference(Table.MessageIdKey, ref message);
            set => SetReference((message = value), Table.MessageIdKey);
        }
    }
}