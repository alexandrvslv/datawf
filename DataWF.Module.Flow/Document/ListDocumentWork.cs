using DataWF.Common;
using DataWF.Data;
using System;
using System.ComponentModel;

namespace DataWF.Module.Flow
{
    public class ListDocumentWork : SelectableList<DocumentWork>
    {
        private Document document;

        public ListDocumentWork(DocumentWorkTable table, Document document)
            : base(table.Select(table.DocumentIdKey, CompareType.Equal, document.Id),
                   new DBComparer<DocumentWork, long?>(table.IdKey))
        {
            this.document = document;
        }

        public override int AddInternal(DocumentWork item)
        {
            if (Contains(item))
                return -1;
            if (item.Document == null)
                item.Document = document;
            var index = base.AddInternal(item);
            item.Attach();
            return index;
        }

        public override void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemPropertyChanged(sender, e);
            if (document != null
                && (string.IsNullOrEmpty(e.PropertyName)
                || string.Equals(e.PropertyName, nameof(DocumentWork.IsComplete), StringComparison.Ordinal)))
            {
                document.RefreshCache();
            }
        }
    }
}

