using DataWF.Data;

namespace DataWF.Module.Flow
{
    public class DocumentReferenceList : DBTableView<DocumentReference>
    {
        private readonly Document doc;
        private readonly DocumentReferenceMode mode = DocumentReferenceMode.None;

        public DocumentReferenceList(DocumentReferenceTable<DocumentReference> table, string filter = "", DBViewKeys mode = DBViewKeys.None)
            : base(table, filter, mode)
        {
        }

        public DocumentReferenceList(DocumentReferenceTable<DocumentReference> table, Document document, DocumentReferenceMode Mode)
            : this(table, Mode == DocumentReferenceMode.Refed
                   ? $"{table.DocumentIdKey.Name}={document.PrimaryId}"
                : Mode == DocumentReferenceMode.Refing
                   ? $"{table.ReferenceIdKey.Name}={document.PrimaryId}"
                   : $"{table.DocumentIdKey.Name}={document.PrimaryId} or {table.ReferenceIdKey.Name}={document.PrimaryId}", DBViewKeys.Static)
        {
            this.doc = document;
            this.mode = Mode;
        }

        public Document Document
        {
            get { return doc; }
        }

        public DocumentReferenceMode Mode
        {
            get { return mode; }
        }

        public override int AddInternal(DocumentReference item)
        {
            if (doc != null)
            {
                if (mode == DocumentReferenceMode.Refed && item.Document == null)
                    item.Document = doc;
                else if (mode == DocumentReferenceMode.Refing && item.Reference == null)
                    item.Reference = doc;
            }
            return base.AddInternal(item);
            //if (mode == DocumentReferenceListMode.Refed && item.Reference != null && !item.Reference.Refing.Contains(item))
            //{
            //    item.Reference.Refing.Add(item);
            //}
            //if (mode == DocumentReferenceListMode.Refing && item.Document != null && !item.Document.Refed.Contains(item))
            //{
            //    item.Document.Refed.Add(item);
            //}
        }

        public override bool Remove(DocumentReference item)
        {
            bool flag = base.Remove(item);
            //if (mode == DocumentReferenceListMode.Refed && item.Reference != null && item.Reference.Refing.Contains(item))
            //{
            //    item.Reference.Refing.Remove(item);
            //}
            //if (mode == DocumentReferenceListMode.Refing && item.Document != null && item.Document.Refed.Contains(item))
            //{
            //    item.Document.Refed.Remove(item);
            //}

            return flag;
        }
    }
}
