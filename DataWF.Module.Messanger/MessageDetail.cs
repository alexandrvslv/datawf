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

        public MessageDetail(DBTable table) : base(table)
        { }

        public IMessageDetailTable MessageDetailTable => (IMessageDetailTable)Table;

        [Browsable(false)]
        [DataMember, Column("message_id")]
        public long? MessageId
        {
            get => GetValue<long?>(MessageDetailTable.MessageIdKey);
            set => SetValue(value, MessageDetailTable.MessageIdKey);
        }

        [Reference(nameof(MessageId))]
        public Message Message
        {
            get { return GetReference(MessageDetailTable.MessageIdKey, ref message); }
            set { SetReference((message = value), MessageDetailTable.MessageIdKey); }
        }
    }
}