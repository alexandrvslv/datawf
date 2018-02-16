using DataWF.Common;
using System;
using System.ComponentModel;
using System.Linq;

namespace DataWF.Data
{
    public class DBColumnReferenceList : SelectableList<DBColumnReference>
    {
        [NonSerialized]
        private string names;

        public DBColumnReferenceList()
        {
            //Indexes.Add("Column");
        }

        public string Names
        {
            get { return names; }
        }

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
            names = string.Empty;
            foreach (var item in this.items)
            {
                names += item.ColumnName.Substring(item.ColumnName.LastIndexOf('.') + 1);
                if (!IsLast(item))
                    names += ", ";
            }
            base.OnListChanged(type, newIndex, oldIndex, property);
        }
    }
}
