using DataWF.Common;
using DataWF.Data;
using System.ComponentModel;

namespace DataWF.Module.Flow
{
    [AbstractTable, InvokerGenerator]
    public abstract partial class DocumentItem : DBItem, IDocumentDetail
    {
        protected Document document;

        protected DocumentItem(DBTable table) : base(table)
        {
        }

        public IDocumentItemTable DocumentItemTable => (IDocumentItemTable)Table;

        [Browsable(false)]
        [Column("document_id")]
        public virtual long? DocumentId
        {
            get => GetValue<long?>(DocumentItemTable.DocumentIdKey);
            set => SetValue(value, DocumentItemTable.DocumentIdKey);
        }

        [Reference(nameof(DocumentId))]
        public Document Document
        {
            get => GetReference(DocumentItemTable.DocumentIdKey, ref document);
            set => SetReference(document = value, DocumentItemTable.DocumentIdKey);
        }


    }
}
