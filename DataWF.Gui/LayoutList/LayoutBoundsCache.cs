using System;
using System.Collections.Generic;
using Xwt;

namespace DataWF.Gui
{
    public partial class LayoutList
    {
        internal class LayoutBoundsCache : Dictionary<LayoutColumn, Rectangle>
        {
            public int Index = -2;
            public LayoutGroup Group;
            public Rectangle Clip = new Rectangle();
            public Rectangle Columns = new Rectangle();
            public Rectangle TempColumns = new Rectangle();
            public Rectangle Content = new Rectangle();
            public Rectangle TempContent = new Rectangle();
            public Rectangle Area = new Rectangle();
            public Rectangle TempArea = new Rectangle();
            public Rectangle Cell = new Rectangle();
            public Rectangle CellGlyph = new Rectangle();
            public Rectangle CellText = new Rectangle();
            public Rectangle ColumnGlyph = new Rectangle();
            public Rectangle ColumnFilter = new Rectangle();
            public Rectangle ColumnSort = new Rectangle();
            public Rectangle ColumnText = new Rectangle();
            public Rectangle Editor = new Rectangle();
            public Rectangle Row = new Rectangle();
            public Rectangle RowHeader = new Rectangle();
            public Rectangle Aggregate = new Rectangle();
            public Rectangle CacheRow = new Rectangle();
            public Rectangle GroupHeader = new Rectangle();
            public Rectangle GroupGlyph = new Rectangle();
            public Rectangle Middle = new Rectangle();
            public Rectangle Filter = new Rectangle();
            public Rectangle Selection = new Rectangle(-1D, -1D, -1D, -1D);
            public List<LayoutColumn> VisibleColumns;

            public new void Clear()
            {
                ClearItems();
                Index = -1;
                Columns = Rectangle.Zero;
                TempArea = Rectangle.Zero;
                TempColumns = Rectangle.Zero;
                TempContent = Rectangle.Zero;
            }

            public void ClearItems()
            {
                base.Clear();
            }
        }
    }
}

