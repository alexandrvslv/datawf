using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace DataWF.Data
{
    public class DBForeignKey : DBConstraint
    {
        [NonSerialized]
        private DBColumn _rcolumn;

        public DBForeignKey()
        {
            Type = DBConstaintType.Foreign;
        }

        public DBForeignKey(DBColumn column, DBTable value) : this()
        {
            Column = column;
            Reference = value.PrimaryKey;
        }

        public override void GenerateName()
        {
            name = string.Format("{0}{1}{2}{3}", Table.Name, Column.Name, Reference.Table.Name, Reference.Name);
        }


        public DBColumn Reference
        {
            get
            {
                if (_rcolumn == null)
                    _rcolumn = DBService.ParseColumn(value, schema);
                return _rcolumn;
            }
            set
            {
                if (Reference != value)
                {
                    Value = value?.FullName;
                    _rcolumn = value;
                }
            }
        }

        [Browsable(false)]
        public DBTable ReferenceTable
        {
            get { return Reference?.Table; }

        }

        [Browsable(false)]
        public string ReferenceTableName
        {
            get { return value == null ? null : Value.Substring(0, value.LastIndexOf(".", StringComparison.Ordinal)); }
        }



        public override string ToString()
        {
            return string.Format("{0}-{1}({2})", Column, ReferenceTable, Reference);
        }
    }
}
