using DataWF.Data;
using DataWF.Module.Flow;
using DataWF.Module.Messanger;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [DataContract, Table("ddocument_comment", "Document", BlockSize = 400, IsLoging = false)]
    public class DocumentComment : DocumentDetail
    {
        private Message message;

        public static DBTable<DocumentComment> DBTable => GetTable<DocumentComment>();

        public DocumentComment()
        {
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetProperty<long?>(); }
            set { SetProperty(value); }
        }

        [Index("ddocument_comment_document_id")]
        public override long? DocumentId { get => base.DocumentId; set => base.DocumentId = value; }

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
            set { message = SetPropertyReference(value); }
        }
    }
}
