using System;
using System.Collections.Generic;
using DataWF;
using Avalonia;

namespace DataWF.Gui
{
    public class LayoutListDrawArgs : EventArgs
    {
        public LayoutList LayoutList;
        public GraphContext Context;
        public Rect Bound;
        public LayoutColumn Column;
        public CellDisplayState State;
        public int Index;
        public object Item;
        public object Value;
        public object Formated;
        public Rect RowBound;
        public List<LayoutColumn> Columns;
        public CellStyle Style;
        public LayoutGroup Group;
        public int DisplayIndex;

        public int GridIndex;
    }
}