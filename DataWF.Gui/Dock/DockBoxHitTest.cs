using System;
using Xwt;
using DataWF.Common;

namespace DataWF.Gui
{
    public class DockBoxHitTest
    {
        DockItem item;
        LayoutAlignType align;
        Rectangle alignBound;


        public DockItem Item
        {
            get { return item; }
            set { item = value; }
        }

        public LayoutAlignType Align
        {
            get { return align; }
            set { align = value; }
        }

        public Rectangle AlignBound
        {
            get { return alignBound; }
            set { alignBound = value; }
        }

        public Rectangle Bound { get; internal set; }
    }
}

