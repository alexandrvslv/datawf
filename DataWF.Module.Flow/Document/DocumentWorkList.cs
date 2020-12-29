using DataWF.Data;
using System.ComponentModel;

namespace DataWF.Module.Flow
{
    public class DocumentWorkList : DBTableView<DocumentWork>
    {
        readonly Document document;

        public DocumentWorkList(DocumentWorkTable table, string filter = "", DBViewKeys mode = DBViewKeys.None)
            : base(table, filter, mode)
        {
            ApplySortInternal(new DBComparer<DocumentWork, long?>(table.IdKey, ListSortDirection.Ascending));
        }

        public DocumentWorkList(DocumentWorkTable table, Document document)
            : this(table, table.DocumentIdKey.Name + "=" + document.Id, DBViewKeys.None)
        {
            this.document = document;
        }

        public override int AddInternal(DocumentWork item)
        {
            if (document != null && item.DocumentId == null)
                item.Document = document;
            return base.AddInternal(item);
        }

        public override string ToString()
        {
            string s = "";
            foreach (DocumentWork tos in this)
                s += tos.ToString() + "\n";
            return s.TrimEnd(new char[] { '\n' });
        }
    }
}

