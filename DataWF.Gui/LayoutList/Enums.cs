using DataWF.Common;
using System;
using Xwt.Drawing;
using System.Reflection;

namespace DataWF.Gui
{
    public delegate ILayoutCellEditor PListGetEditorHandler(object listItem, object value, ILayoutCell cell);

    public delegate CellStyle PListGetStyleHandler(object sender, object listItem, object value, ILayoutCell cell);

    public class PListEventArgs : EventArgs
    {
        public object ListItem { get; set; }
        public object Value { get; set; }
        public ILayoutCell Cell { get; set; }
    }

    public interface IGlyph
    {
        GlyphType Glyph { get; set; }
        Image Image { get; set; }
    }

    public interface IText
    {
        string Text { get; set; }
        event EventHandler TextChanged;
    }

    public enum CollectedType
    {
        None,
        Sum,
        Min,
        Max,
        Avg,
        Custom
    }

    public enum LayoutHitTestLocation
    {
        None,
        Column,
        Cell,
        Row,
        Header,
        Group,
        Intermediate,
        Aggregate
    }

    public enum LayoutHitTestCellLocation
    {
        None,
        Image,
        Check,
        Glyph,
        Text,
        Sort,
        Filter
    }

    public enum LayoutListMode
    {
        List,
        Tree,
        Fields,
        Grid,
    }

    [Flags]
    public enum LayoutListInfoKeys
    {
        None = 0,
        ColumnsVisible = 1,
        CollectingRow = 2,
        HeaderVisible = 4,
        HeaderFrosen = 8,
        GroupHeader = 16,
        GroupName = 32,
        GroupCount = 64,
        CalcHeigh = 128,
        CalcWidth = 256,
        GridAuto = 512,
        GridMode = 1024,
        HotTrackingRow = 2048,
        HotTrackingCell = 4096,
        HotSelectRow = 8192,
        ShowToolTip = 16384
    }

    public enum CellStyleBrushType
    {
        Solid,
        Texture,
        Gradient
    }

    [Flags]
    public enum LayoutListKeys
    {
        None = 0,
        //allows
        AllowFilter = 1 << 0,
        AllowSort = 1 << 1,
        AllowCheck = 1 << 2,
        AllowMoveColumn = 1 << 3,
        AllowSizeColumn = 1 << 4,
        AllowSizeCell = 1 << 5,
        AllowSizeHeader = 1 << 6,
        AllowEditColumn = 1 << 7,
        AllowImage = 1 << 8,
        //auto
        AutoNameFill = 1 << 9,
        AutoNameSort = 1 << 10,
        AutoNameHide = 1 << 11,
        //generating
        GenerateFields = 1 << 12,
        GenerateColumns = 1 << 13,
        GenerateName = 1 << 14,
        GenerateMenu = 1 << 15,
        //
        CheckRecursive = 1 << 16,
        CheckClearBase = 1 << 17
    }

    public enum Orientation
    {
        Horizontal,
        Vertical
    }

    public enum LayoutSelectionMode
    {
        None,
        Row,
        Column,
        Group,
        Header,
        Aggregate
    }

    public enum LayoutSelectionChange
    {
        Add,
        Remove,
        Reset,
        Hover,
        Cell
    }

    public enum LayoutListState
    {
        Default,
        MoveColumn,
        DragDrop,
        SizeColumWidth,
        SizeColumHeight,
        SizeHeaderWidth,
        Select
    }
}
