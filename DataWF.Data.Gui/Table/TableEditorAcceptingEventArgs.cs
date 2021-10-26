using DataWF.Data;
using DataWF.Gui;

namespace DataWF.Data.Gui
{
    public class TableEditorAcceptingEventArgs : ListEditorEventArgs
    {
        private DBItem childRow = null;
        private string childColumn = "";

        public TableEditorAcceptingEventArgs(DBItem row, DBItem childRow, string childColumn)
            : base(row)
        {
            this.childRow = childRow;
            this.childColumn = childColumn;
        }

        public DBItem ChildRow
        {
            get { return childRow; }
            set { childRow = value; }
        }

        public string ChildColumn
        {
            get { return childColumn; }
            set { childColumn = value; }
        }
    }
}
