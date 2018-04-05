using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class LayoutHitTestInfo
    {
        public Rectangle RowBound = new Rectangle();
        public Rectangle ItemBound = new Rectangle();
        public Rectangle SubItemBound = new Rectangle();
        public Point Point = new Point();

        public LayoutHitTestInfo()
        {
        }

        public bool KeyCtrl { get; set; }

        public bool KeyShift { get; set; }

        public int Index { get; set; }

        public object Item { get; set; }

        public bool MouseDown { get; set; }

        public PointerButton MouseButton { get; set; }

        public LayoutGroup Group { get; set; }

        public LayoutColumn Column { get; set; }

        public LayoutAlignType Anchor { get; set; }

        public LayoutHitTestLocation Location { get; set; }

        public LayoutHitTestCellLocation SubLocation { get; set; }

        public int Clicks { get; set; }
    }
}
