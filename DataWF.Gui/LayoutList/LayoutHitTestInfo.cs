using DataWF;
using Avalonia;
using Avalonia.Input;

namespace DataWF.Gui
{
    public class LayoutHitTestInfo
    {
        public Rect RowBound = new Rect();
        public Rect ItemBound = new Rect();
        public Rect SubItemBound = new Rect();
        public Point Point = new Point();

        public LayoutHitTestInfo()
        {
        }

        public bool KeyCtrl { get; set; }

        public bool KeyShift { get; set; }

        public int Index { get; set; }

        public object Item { get; set; }

        public bool MouseDown { get; set; }

        public MouseButton MouseButton { get; set; }

        public LayoutGroup Group { get; set; }

        public LayoutColumn Column { get; set; }

        public LayoutAlignType Anchor { get; set; }

        public LayoutHitTestLocation Location { get; set; }

        public LayoutHitTestCellLocation SubLocation { get; set; }

        public int Clicks { get; set; }
    }
}
