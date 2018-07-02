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
        public static DBTable<DocumentComment> DBTable => GetTable<DocumentComment>();

        public DocumentComment()
        {
            Build(DBTable);
        }

        [DataMember, Column("unid", Keys = DBColumnKeys.Primary)]
        public long? Id
        {
            get { return GetProperty<long?>(); }
            set { SetProperty(value); }
        }

        [Browsable(false)]
        [DataMember, Column("message_id")]
        public long? MessageId
        {
            get { return GetProperty<int?>(); }
            set { SetProperty(value); }
        }

        [Reference(nameof(MessageId))]
        public Message Message
        {
            get { return GetPropertyReference<Message>(); }
            set { SetPropertyReference(value); }
        }
    }
}
