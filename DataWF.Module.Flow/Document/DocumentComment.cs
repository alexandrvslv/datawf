using DataWF.Data;
using DataWF.Module.Messanger;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [DataContract, Table("ddocument_comment", "Document", BlockSize = 400, IsLoging = false)]
    public class DocumentComment : DocumentDetail<DocumentComment>
    {
        private static DBColumn messageKey = DBColumn.EmptyKey;
        public static DBColumn MessageKey => DBTable.ParseProperty(nameof(MessageId), ref messageKey);

        private Message message;

        public DocumentComment()
        {
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }

        [Index("ddocument_comment_document_id")]
        public override long? DocumentId { get => base.DocumentId; set => base.DocumentId = value; }

        [Browsable(false)]
        [DataMember, Column("message_id")]
        public long? MessageId
        {
            get { return GetValue<long?>(MessageKey); }
            set { SetValue(value, MessageKey); }
        }

        [Reference(nameof(MessageId))]
        public Message Message
        {
            get { return GetReference(MessageKey, ref message); }
            set { message = SetReference(value, MessageKey); }
        }


    }
}
