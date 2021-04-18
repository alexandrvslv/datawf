using DataWF.Common;
using DataWF.Data;
using DataWF.Module.Messanger;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [Table("ddocument_comment", "Document", BlockSize = 400, Keys = DBTableKeys.NoLogs), InvokerGenerator(Instance = true)]
    public partial class DocumentComment : DocumentItem
    {
        private Message message;

        public DocumentComment(DBTable table) : base(table)
        {
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long Id
        {
            get => GetValue<long>(Table.IdKey);
            set => SetValue(value, Table.IdKey);
        }

        [Browsable(false)]
        [Column("message_id")]
        public long? MessageId
        {
            get => GetValue<long?>(Table.MessageIdKey);
            set => SetValue(value, Table.MessageIdKey);
        }

        [Reference(nameof(MessageId))]
        public Message Message
        {
            get => GetReference(Table.MessageIdKey, ref message);
            set => SetReference(message = value, Table.MessageIdKey);
        }

        protected override void RaisePropertyChanged(string property)
        {
            base.RaisePropertyChanged(property);
            if (Attached)
            {
                document?.OnReferenceChanged(this);
            }
        }


    }
}
