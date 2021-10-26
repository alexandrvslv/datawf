using System;
using System.ComponentModel;

namespace DataWF.Gui
{
    public class LayoutHitTestEventArgs : CancelEventArgs
    {
        public LayoutHitTestEventArgs() : base() { }
        public LayoutHitTestInfo HitTest { get; set; }
    }

    public class LayoutListItemEventArgs : EventArgs
    {
        public object Item { get; set; }
    }

}
