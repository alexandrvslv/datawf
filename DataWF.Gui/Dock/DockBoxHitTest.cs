using System;
using Avalonia;
using DataWF;

namespace DataWF.Gui
{
    public class DockBoxHitTest
    {
        DockItem item;
        LayoutAlignType align;
        Rect alignBound;


        public DockItem Item
        {
            get => item;
            set => item = value;
        }

        public LayoutAlignType Align
        {
            get => align;
            set => align = value;
        }

        public Rect AlignBound
        {
            get => alignBound;
            set => alignBound = value;
        }

        public Rect Bound { get; internal set; }
    }
}

