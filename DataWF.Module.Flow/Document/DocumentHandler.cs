using System;
using System.ComponentModel;

namespace DataWF.Module.Flow
{
    public class DocumentHandler : INotifyPropertyChanged
    {
        [NonSerialized()]
        private Document document;
        private string id;
        private DateTime date = DateTime.Now;

        public DocumentHandler()
        {
        }

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

        public string Id
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
                if (document == null && id != null)
                    document = Document.DBTable.LoadById(id);
                return document;
            }
            set
            {
                Id = value?.PrimaryId.ToString();
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
