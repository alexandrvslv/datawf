using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;

namespace DataWF.Data
{
    public class DBColumnReference : IContainerNotifyPropertyChanged
    {
        private string columnName;
        private DBColumn column;

        [Browsable(false)]
        public string ColumnName
        {
            get { return columnName; }
            set
            {
                if (columnName != value)
                {
                    columnName = value;
                    column = null;
                    OnPropertyChanged(nameof(ColumnName));
                }
            }
        }

        [XmlIgnore]
        public DBColumn Column
        {
            get { return column ?? DBService.ParseColumn(columnName); }
            set
            {
                if (Column != value)
                {
                    ColumnName = value?.FullName;
                    column = value;
                }
            }
        }

        [Browsable(false), XmlIgnore]
        public INotifyListChanged Container { get; set; }


        [Browsable(false), XmlIgnore]
        public DBColumnReferenceList List
        {
            get { return Container as DBColumnReferenceList; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            var arg = new PropertyChangedEventArgs(property);
            PropertyChanged?.Invoke(this, arg);
            Container?.OnPropertyChanged(this, arg);
        }

        public override string ToString()
        {
            return Column?.ToString() ?? columnName;
        }

        public DBColumnReference Clone()
        {
            return new DBColumnReference { ColumnName = ColumnName };
        }
    }
}
