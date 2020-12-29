using DataWF.Common;

namespace DataWF.Module.Flow
{
    public class ListDocumentData : SelectableList<DocumentData>
    {
        private readonly DocumentDataTable<DocumentData> table;
        private readonly Document document;

        public ListDocumentData(DocumentDataTable<DocumentData> table, Document document)
            : base(table.Select(table.DocumentIdKey, CompareType.Equal, document.PrimaryId))
        {
            this.table = table;
            this.document = document;
        }

        public override int AddInternal(DocumentData item)
        {
            if (Contains(item))
                return -1;
            if (item.Document == null)
                item.Document = document;
            int index = base.AddInternal(item);
            item.Attach();
            return index;
        }
    }
}
