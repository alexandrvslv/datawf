using DataWF.Common;
using DataWF.Gui;

namespace DataWF.Data.Gui
{
    //idea from mvd ais
    public class DataFieldEditor : FieldEditor
    {
        public DataFieldEditor()
        {
        }

        public DBColumn Column
        {
            get { return Invoker as DBColumn; }
        }

        public override object ReadValue(object listItem)
        {
            if (listItem is DBItem && Invoker.Name.IndexOf('.') >= 0)
            {
                return ((DBItem)listItem)[Invoker.Name];
            }
            return base.ReadValue(listItem);
        }

        public void BindData(DBItem dataSource, DBColumn column, DBTable refer = null)
        {
            if (Column != column && dataSource?.GetType() != DataSource.GetType())
            {
                Binding?.Dispose();
                if (column != null)
                {
                    CellEditor = TableLayoutList.InitCellEditor(column);
                    if (CellEditor is CellEditorTable && refer != null)
                    {
                        ((CellEditorTable)CellEditor).Table = refer;
                    }
                    Binding = new InvokeBinder<DBItem, FieldEditor>(dataSource, column, this, EmitInvoker.Initialize<FieldEditor>(nameof(Value)));
                }
            }
            DataSource = dataSource;
        }

        public override void Localize()
        {
            if (Column != null)
            {
                label.Text = Column.Name;
            }
            else
            {
                base.Localize();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
