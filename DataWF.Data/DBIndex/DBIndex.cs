using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DataWF.Data
{
    public class DBIndex : DBTableItem
    {
        private bool unique;
        private DBColumnReferenceList columns = new DBColumnReferenceList();

        public DBIndex()
        {
        }

        public bool Unique
        {
            get { return unique; }
            set
            {
                if (unique = value)
                    return;
                unique = value;
                OnPropertyChanged(nameof(Unique), true);
            }
        }

        public DBColumnReferenceList Columns
        {
            get { return columns; }
            set { columns = value; }
        }

        public override object Clone()
        {
            var index = new DBIndex()
            {
                Name = name,
                Unique = Unique
            };
            foreach (var column in Columns)
            {
                index.Columns.Add(column.Clone());
            }
            return index;
        }

        public override string FormatSql(DDLType ddlType)
        {
            throw new NotImplementedException();
        }
    }
}
