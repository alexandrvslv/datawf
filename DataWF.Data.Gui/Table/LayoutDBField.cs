using DataWF.Gui;


namespace DataWF.Data.Gui
{
    public class LayoutDBField : LayoutField
    {
        public LayoutDBField OwnerField
        {
            get { return (LayoutDBField)Owner; }
        }

        public DBColumn Column
        {
            get { return Invoker as DBColumn; }
        }

        public DBItem GetReference(DBItem item)
        {
            if (OwnerField != null)
                item = OwnerField.GetReference(item);
            return item?.GetReference(Column);
        }

        public override object ReadValue(object listItem)
        {
            var dbItem = listItem as DBItem;
            if (OwnerField != null)
            {
                dbItem = OwnerField.GetReference(dbItem);
            }
            return dbItem?.GetValue(Column);
        }

        public override void WriteValue(object listItem, object value)
        {
            var dbItem = listItem as DBItem;
            if (OwnerField != null)
            {
                dbItem = OwnerField.GetReference(dbItem);
            }
            if (dbItem != null)
            {
                dbItem[Column] = value;
            }
        }
    }

}
