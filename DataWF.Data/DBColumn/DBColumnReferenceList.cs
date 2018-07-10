using DataWF.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace DataWF.Data
{
    public class DBColumnReferenceList : SelectableList<DBColumnReference>
    {
        public DBColumnReferenceList()
        {
            //Indexes.Add("Column");
        }

        public DBColumnReferenceList(IEnumerable<DBColumn> columns) : this()
        {
            AddRangeInternal(columns.Select(p => new DBColumnReference { Column = p }));
        }

        [XmlIgnore, JsonIgnore]
        public string Names { get; private set; }

        public void Add(DBColumn column)
        {
            Add(new DBColumnReference { Column = column });
        }

        public void Add(string column)
        {
            Add(new DBColumnReference { ColumnName = column });
        }

        public bool Contains(DBColumn column)
        {
            return Contains(column.FullName);
        }

        public bool Contains(string column)
        {
            return Select(nameof(DBColumnReference.ColumnName), CompareType.Equal, column).Any();
        }

        protected override void OnPropertyChanged(string property)
        {
            Names = string.Empty;
            foreach (var element in items)
            {
                Names += element.ColumnName.Substring(element.ColumnName.LastIndexOf('.') + 1);
                if (!IsLast(element))
                    Names += ", ";
            }
            base.OnPropertyChanged(property);
        }
    }
}
