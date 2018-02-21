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

        public DBIndex()
        {
            Columns = new DBColumnReferenceList();
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

        public DBColumnReferenceList Columns { get; set; }

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
            var builder = new StringBuilder();
            Schema?.System?.Format(builder, this, ddlType);
            return builder.ToString();
        }
    }
}
