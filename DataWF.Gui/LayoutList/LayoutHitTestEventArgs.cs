using System.ComponentModel;

namespace DataWF.Gui
{
    public class LayoutHitTestEventArgs : CancelEventArgs
    {
        public LayoutHitTestEventArgs() : base() { }
        public LayoutHitTestInfo HitTest { get; set; }
    }

}
