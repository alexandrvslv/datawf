using System.ComponentModel;

namespace DataWF.Common
{
    [System.Runtime.InteropServices.ComVisible(false)]
    public class ListPropertyChangedEventArgs : ListChangedEventArgs
    {
        public ListPropertyChangedEventArgs(ListChangedType type, int newIndex, int oldIndex)
            : base(type, newIndex, oldIndex)
        { }

        public object Sender { get; set; }

        public string Property { get; set; }
    }
}
