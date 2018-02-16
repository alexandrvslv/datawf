using DataWF.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public abstract class DBTableItem : DBSchemaItem, IDBTableContent
    {
        protected string table;
        [NonSerialized()]
        private DBTable _table = null;

        [Browsable(false)]
        public string TableName
        {
            get { return table; }
            set
            {
                if (value != table)
                {
                    table = value;
                    OnPropertyChanged(nameof(TableName), true);
                }
            }
        }

        [XmlIgnore]
        public DBTable Table
        {
            get { return _table ?? (_table = DBService.ParseTable(table, schema)); }
            set
            {
                if (_table != value)
                {
                    table = value?.FullName;
                    _table = value;
                    schema = value?.Schema;
                    OnPropertyChanged(nameof(Table), true);
                }
            }
        }

    }
}
