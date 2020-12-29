using System;
using System.ComponentModel;

namespace DataWF.Module.Flow
{
    public class DocumentHandler : INotifyPropertyChanged
    {
        [NonSerialized()]
        private Document document;
        private long id;
        private DateTime date = DateTime.Now;

        public DocumentHandler()
        {
        }
        DocumentTable<Document> DocumentTable { get; set; }
        public override string ToString()
        {
            return Document == null ? base.ToString() : Document.ToString();
        }

        public DateTime Date
        {
            get => date;
            set
            {
                if (date == value)
                    return;
                date = value;
                OnPropertyChanged(nameof(Date));
            }
        }

        public long Id
        {
            get => id;
            set
            {
                if (id == value)
                    return;
                id = value;
                document = null;
                OnPropertyChanged(nameof(Id));
            }
        }

        public Document Document
        {
            get
            {
                if (document == null && id != 0)
                    document = DocumentTable.LoadById(id);
                return document;
            }
            set
            {
                Id = value?.Id ?? 0;
                document = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
