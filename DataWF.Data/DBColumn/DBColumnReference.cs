using DataWF.Common;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBColumnReference : IContainerNotifyPropertyChanged
    {
        private string columnName;
        private DBColumn column;

        [XmlIgnore, JsonIgnore]
        public DBSchema Schema { get { return List?.Schema; } }

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
            get { return column ?? DBService.Schems.ParseColumn(columnName, Schema); }
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
        public IEnumerable<INotifyListPropertyChanged> Containers => TypeHelper.GetContainers(PropertyChanged);

        [Browsable(false), XmlIgnore, JsonIgnore]
        public DBColumnReferenceList List
        {
            get { return Containers.FirstOrDefault() as DBColumnReferenceList; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
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
