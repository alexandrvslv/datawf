using System.ComponentModel;

namespace DataWF.Gui
{
    public class LayoutValueEventArgs : CancelEventArgs
    {
        private object listItem;
        private object data;
        private ILayoutCell cell;

        public LayoutValueEventArgs()
            : base()
        { }

        public LayoutValueEventArgs(object listItem, object data, ILayoutCell column)
            : this()
        {
            this.listItem = listItem;
            this.data = data;
            this.cell = column;
        }
        public object Data
        {
            get { return data; }
            set { data = value; }
        }

        public ILayoutCell Cell
        {
            get { return cell; }
            set { cell = value; }
        }

        public object ListItem
        {
            get { return listItem; }
            set { listItem = value; }
        }
    }
}

