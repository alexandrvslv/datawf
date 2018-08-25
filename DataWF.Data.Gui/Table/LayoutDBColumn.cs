using DataWF.Gui;


namespace DataWF.Data.Gui
{
    public class LayoutDBColumn : LayoutColumn
    {
        public LayoutDBColumn OwnerColumn
        {
            get { return (LayoutDBColumn)Owner; }
        }

        public DBColumn DBColumn
        {
            get { return Invoker as DBColumn; }
        }

        public DBItem GetReference(DBItem item)
        {
            if (OwnerColumn != null)
                item = OwnerColumn.GetReference(item);
            return item?.GetReference(DBColumn);
        }

        public override object ReadValue(object listItem)
        {
            var dbItem = listItem as DBItem;
            if (OwnerColumn != null)
            {
                dbItem = OwnerColumn.GetReference(dbItem);
            }
            return dbItem?.GetValue(DBColumn);
        }

        public override void WriteValue(object listItem, object value)
        {
            var dbItem = listItem as DBItem;
            if (OwnerColumn != null)
            {
                dbItem = OwnerColumn.GetReference(dbItem);
            }
            if (dbItem != null)
                dbItem.SetValue(value, DBColumn);
        }
    }

}
