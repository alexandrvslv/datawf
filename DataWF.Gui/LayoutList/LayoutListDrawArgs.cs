using System;
using System.Collections.Generic;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class LayoutListDrawArgs : EventArgs
    {
        public LayoutList LayoutList;
        public GraphContext Context;
        public Rectangle Bound;
        public LayoutColumn Column;
        public CellDisplayState State;
        public int Index;
        public object Item;
        public object Value;
        public object Formated;
        public Rectangle RowBound;
        public List<LayoutColumn> Columns;
        public CellStyle Style;
        public LayoutGroup Group;
        public int DisplayIndex;

        public int GridIndex;
    }
}