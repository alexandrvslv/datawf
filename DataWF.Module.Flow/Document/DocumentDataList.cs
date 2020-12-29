using DataWF.Common;
using DataWF.Data;

namespace DataWF.Module.Flow
{
    public class DocumentDataList : DBTableView<DocumentData>
    {
        readonly Document document;

        public DocumentDataList(DocumentDataTable<DocumentData> table, Document document)
            : this(table, table.DocumentIdKey.Name + "=" + document.PrimaryId, DBViewKeys.Static)
        {
            this.document = document;
        }

        public DocumentDataList(DocumentDataTable<DocumentData> table, string filter, DBViewKeys mode = DBViewKeys.None)
            : base(table, filter, mode)
        {
        }

        public DocumentDataTable<DocumentData> DocumentDataTable => (DocumentDataTable<DocumentData>)Table;

        public override int AddInternal(DocumentData item)
        {
            if (document != null && item.Document == null)
                item.Document = document;
            return base.AddInternal(item);
        }

        public void FilterByDocument(Document document)
        {
            DefaultParam = new QParam(LogicType.And, DocumentDataTable.DocumentIdKey, CompareType.Equal, document.PrimaryId);
        }
    }
}
