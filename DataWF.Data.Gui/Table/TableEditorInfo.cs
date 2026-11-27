namespace DataWF.Data.Gui
{
    public class TableEditorInfo
    {
        public DBItem Item { get; set; }

        public DBColumn Column { get; set; }

        public IDBTableView TableView { get; set; }

        public TableEditorMode Mode { get; set; }

        public bool ReadOnly { get; set; }

        public TableEditorStatus Status { get; set; }

        public IDBTable Table { get; set; }

        public void Dispose()
        {
            TableView?.Dispose();
        }
    }
}
