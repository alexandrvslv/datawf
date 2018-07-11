using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;
using Newtonsoft.Json;

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

        [XmlIgnore, JsonIgnore]
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

        [Browsable(false), XmlIgnore, JsonIgnore]
        public INotifyListPropertyChanged Container { get; set; }


        [Browsable(false), XmlIgnore, JsonIgnore]
        public DBColumnReferenceList List
        {
            get { return Container as DBColumnReferenceList; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            Container?.OnPropertyChanged(this, property);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
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
