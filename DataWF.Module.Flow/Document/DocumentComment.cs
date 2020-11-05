using DataWF.Data;
using DataWF.Module.Messanger;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [Table("ddocument_comment", "Document", BlockSize = 400, Keys = DBTableKeys.NoLogs)]
    public class DocumentComment : DBItem, IDocumentDetail
    {   
        public static readonly DBTable<DocumentComment> DBTable = GetTable<DocumentComment>();
        public static readonly DBColumn MessageKey = DBTable.ParseProperty(nameof(MessageId));
        public static readonly DBColumn DocumentKey = DBTable.ParseProperty(nameof(DocumentId));

        private Message message;

        public DocumentComment()
        {
        }

        private Document document;
        [Browsable(false)]
        [Column("document_id"), Index("ddocument_comment_document_id")]
        public virtual long? DocumentId
        {
            get => GetValue<long?>(DocumentKey);
            set => SetValue(value, DocumentKey);
        }

        [Reference(nameof(DocumentId))]
        public Document Document
        {
            get => GetReference(DocumentKey, ref document);
            set => SetReference(document = value, DocumentKey);
        }

        [Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get => GetValue<long?>(Table.PrimaryKey);
            set => SetValue(value, Table.PrimaryKey);
        }


        [Browsable(false)]
        [Column("message_id")]
        public long? MessageId
        {
            get => GetValue<long?>(MessageKey);
            set => SetValue(value, MessageKey);
        }

        [Reference(nameof(MessageId))]
        public Message Message
        {
            get => GetReference(MessageKey, ref message);
            set => SetReference(message = value, MessageKey);
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
