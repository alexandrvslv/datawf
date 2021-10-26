using System.ComponentModel;

namespace DataWF.Gui
{
    public class ListEditorEventArgs : CancelEventArgs
    {
        public ListEditorEventArgs()
            : this(null)
        { }

        public ListEditorEventArgs(object item)
        {
            Item = item;
        }

        public object Item { get; set; }
    }
}

