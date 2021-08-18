using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;

namespace DataWF.Module.Flow
{
    [AbstractTable, InvokerGenerator]
    public abstract partial class DocumentItem : DBItem, IDocumentDetail
    {
        protected Document document;

        [Browsable(false)]
        [Column("document_id")]
        public virtual long? DocumentId
        {
            get => GetValue<long?>(Table.DocumentIdKey);
            set => SetValue(value, Table.DocumentIdKey);
        }

        [Reference(nameof(DocumentId))]
        public Document Document
        {
            get => GetReference(Table.DocumentIdKey, ref document);
            set => SetReference(document = value, Table.DocumentIdKey);
        }


    }
}
