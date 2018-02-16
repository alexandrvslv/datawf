using System;
using Xwt;
using DataWF.Common;

namespace DataWF.Gui
{
    public class DockBoxHitTest
    {
        DockMapItem item;
        LayoutAlignType align;
        Rectangle alignBound;


        public DockMapItem Item
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

