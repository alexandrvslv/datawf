namespace DataWF.Data.Gui
{
    public class TableEditorInfo
    {
        public DBItem Item { get; set; }

        public DBColumn Column { get; set; }

        public IDBTableView TableView { get; set; }

        public TableFormMode Mode { get; set; }

        public bool ReadOnly { get; set; }

        public TableControlStatus Status { get; set; }

        public DBTable Table { get; set; }

        public void Dispose()
        {
            TableView?.Dispose();
        }
    }
}
