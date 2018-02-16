using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class LayoutHitTestInfo
    {
        private PointerButton mouseButton;
        private LayoutGroup gp;
        private LayoutColumn column;
        private int index;
        private LayoutAlignType anchor;
        private LayoutHitTestLocation location;
        private LayoutHitTestCellLocation subLocation;

        public Rectangle RowBound = new Rectangle();
        public Rectangle ItemBound = new Rectangle();
        public Rectangle SubItemBound = new Rectangle();
        public Point Point = new Point();

        public LayoutHitTestInfo()
        {
        }

        public bool KeyCtrl { get; set; }

        public bool KeyShift { get; set; }

        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        public object Item
        {
            get;
            set;
        }

        public bool MouseDown { get; set; }

        public PointerButton MouseButton
        {
            get { return mouseButton; }
            set { mouseButton = value; }
        }

        public LayoutGroup Group
        {
            get { return gp; }
            set { gp = value; }
        }

        public LayoutColumn Column
        {
            get { return column; }
            set { column = value; }
        }

        public LayoutAlignType Anchor
        {
            get { return anchor; }
            set { anchor = value; }
        }

        public LayoutHitTestLocation Location
        {
            get { return location; }
            set { location = value; }
        }

        public LayoutHitTestCellLocation SubLocation
        {
            get { return subLocation; }
            set { subLocation = value; }
        }
    }
}
