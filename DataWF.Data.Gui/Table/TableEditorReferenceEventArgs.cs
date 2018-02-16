using System;
using DataWF.Data;

namespace DataWF.Data.Gui
{
    public class TableEditorReferenceEventArgs : EventArgs
    {
        private DBForeignKey relation;

        public TableEditorReferenceEventArgs(DBForeignKey relation)
        {
            this.relation = relation;
        }

        public DBForeignKey Relation
        {
            get { return this.relation; }
            set { relation = value; }
        }
    }
}
