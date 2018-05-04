using DataWF.Common;
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

        [XmlIgnore]
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

        public override void OnListChanged(ListChangedType type, int newIndex = -1, int oldIndex = -1, string property = null)
        {
            Names = string.Empty;
            foreach (var item in items)
            {
                Names += item.ColumnName.Substring(item.ColumnName.LastIndexOf('.') + 1);
                if (!IsLast(item))
                    Names += ", ";
            }
            base.OnListChanged(type, newIndex, oldIndex, property);
        }
    }
}
