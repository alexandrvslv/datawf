using System;
using System.Collections.Generic;
using Avalonia;

namespace DataWF.Gui
{
    public partial class LayoutList
    {
        internal class LayoutBoundsCache : Dictionary<LayoutColumn, Rect>
        {
            public int Index = -2;
            public LayoutGroup Group;
            public Rect Clip = new Rect();
            public Rect Columns = new Rect();
            public Rect TempColumns = new Rect();
            public Rect Content = new Rect();
            public Rect TempContent = new Rect();
            public Rect Area = new Rect();
            public Rect TempArea = new Rect();
            public Rect Cell = new Rect();
            public Rect CellGlyph = new Rect();
            public Rect CellText = new Rect();
            public Rect ColumnGlyph = new Rect();
            public Rect ColumnFilter = new Rect();
            public Rect ColumnSort = new Rect();
            public Rect ColumnText = new Rect();
            public Rect Editor = new Rect();
            public Rect Row = new Rect();
            public Rect RowHeader = new Rect();
            public Rect Aggregate = new Rect();
            public Rect CacheRow = new Rect();
            public Rect GroupHeader = new Rect();
            public Rect GroupGlyph = new Rect();
            public Rect Middle = new Rect();
            public Rect Filter = new Rect();
            public Rect Selection = new Rect(-1D, -1D, -1D, -1D);
            public List<LayoutColumn> VisibleColumns;

            public new void Clear()
            {
                ClearItems();
                Index = -1;
                Columns = default;
                TempArea = default;
                TempColumns = default;
                TempContent = default;
            }

            public void ClearItems()
            {
                base.Clear();
            }
        }
    }
}

