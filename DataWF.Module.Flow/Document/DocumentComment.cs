using DataWF.Data;
using DataWF.Module.Messanger;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace DataWF.Module.Flow
{
    [DataContract, Table("ddocument_comment", "Document", BlockSize = 400, IsLoging = false)]
    public class DocumentComment : DBItem, IDocumentDetail
    {
        private static DBTable<DocumentComment> dbTable;
        private static DBColumn documentKey = DBColumn.EmptyKey;

        private static DBColumn messageKey = DBColumn.EmptyKey;
        public static DBColumn MessageKey => DBTable.ParseProperty(nameof(MessageId), ref messageKey);
        public static DBColumn DocumentKey => DBTable.ParseProperty(nameof(DocumentId), ref documentKey);

        public static DBTable<DocumentComment> DBTable => dbTable ?? (dbTable = GetTable<DocumentComment>());


        private Message message;

        public DocumentComment()
        {
        }

        private Document document;
        [Browsable(false)]
        [DataMember, Column("document_id"), Index("ddocument_comment_document_id")]
        public virtual long? DocumentId
        {
            get { return GetValue<long?>(DocumentKey); }
            set { SetValue(value, DocumentKey); }
        }

        [Reference(nameof(DocumentId))]
        public Document Document
        {
            get { return GetReference(DocumentKey, ref document); }
            set { SetReference(document = value, DocumentKey); }
        }

        public override void OnPropertyChanged(string property, DBColumn column = null, object value = null)
        {
            base.OnPropertyChanged(property, column, value);
            if (Attached)
            {
                GetReference<Document>(DocumentKey, ref document, DBLoadParam.None)?.OnReferenceChanged(this);
            }
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetValue<long?>(Table.PrimaryKey); }
            set { SetValue(value, Table.PrimaryKey); }
        }


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
            set { SetReference(message = value, MessageKey); }
        }


    }
}
