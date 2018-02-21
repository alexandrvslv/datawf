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
        protected DBTableItem()
        { }

        protected DBTableItem(string name) : base(name)
        { }

        [XmlIgnore, Browsable(false)]
        public DBTable Table { get; set; }

        [Browsable(false)]
        public override DBSchema Schema
        {
            get { return Table?.Schema; }
        }

    }
}
