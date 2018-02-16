using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBColumnReference
    {
        private string column;
        [NonSerialized]
        private DBColumn _column;

        [Browsable(false)]
        public string ColumnName
        {
            get { return column; }
            set
            {
                if (column == value)
                    return;
                column = value;
                _column = null;
            }
        }

        [XmlIgnore]
        public DBColumn Column
        {
            get
            {
                if (_column == null && column != null)
                    _column = DBService.ParseColumn(column);
                return _column;
            }
            set
            {
                if (Column == value)
                    return;
                ColumnName = value?.FullName;
                _column = value;
            }
        }

        public override string ToString()
        {
            return Column != null ? _column.ToString() : column;
        }

        public DBColumnReference Clone()
        {
            return new DBColumnReference()
            {
                Column = Column
            };
        }
    }
}
