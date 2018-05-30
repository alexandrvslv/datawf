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
            if (listItem is DBItem && Property.IndexOf('.') >= 0)
            {
                return ((DBItem)listItem)[Property];
            }
            return base.ReadValue(listItem);
        }

        public void BindData(DBItem dataSource, string column, DBTable refer = null)
        {
            if (Property != column && dataSource != null)
            {
                Property = column;
                Bind = true;
                DBColumn dcolumn = dataSource == null ? null : dataSource.Table.ParseColumn(column);
                if (dcolumn != null)
                {
                    Invoker = dcolumn;
                    CellEditor = TableLayoutList.InitCellEditor(dcolumn);
                    if (CellEditor is CellEditorTable && refer != null)
                    {
                        ((CellEditorTable)CellEditor).Table = refer;
                    }
                }
                else
                {
                    base.BindData(dataSource, column);
                }
            }

            DataSource = dataSource;
            ReadValue();
            Localize();
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

    public class ToolDataFieldEditor : ToolContentItem
    {
        public int fieldWidth = 100;

        public ToolDataFieldEditor()
            : base(new DataFieldEditor())
        {
            Field.Text = string.Empty;
            Field.MinWidth = fieldWidth;
        }

        public DataFieldEditor Field
        {
            get { return base.Content as DataFieldEditor; }
        }

        public int FieldWidth
        {
            get { return fieldWidth; }
            set
            {
                if (fieldWidth != value)
                {
                    fieldWidth = value;
                }
            }
        }

    }
}
