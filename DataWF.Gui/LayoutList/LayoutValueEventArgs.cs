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
            get => data;
            set => data = value;
        }

        public ILayoutCell Cell
        {
            get => cell;
            set => cell = value;
        }

        public object ListItem
        {
            get => listItem;
            set => listItem = value;
        }
    }
}

