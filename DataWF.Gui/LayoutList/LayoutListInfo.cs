using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt;
using System.Xml.Serialization;

namespace DataWF.Gui
{
    public class LayoutListInfo : IDisposable
    {
        LayoutListInfoKeys keys = LayoutListInfoKeys.ColumnsVisible | LayoutListInfoKeys.HeaderVisible | LayoutListInfoKeys.GroupHeader | LayoutListInfoKeys.GroupCount | LayoutListInfoKeys.HotTrackingCell;

        protected string filter;
        protected LayoutColumnMap columns;
        protected LayoutSortList sorters;

        private CellStyle styleGroup;
        private CellStyle styleRow;
        private CellStyle styleCell;
        private CellStyle styleColumn;
        private CellStyle styleHeader;

        private double scale = 1D;
        private int gridCol = 1;
        private GridOrientation gridOrient = GridOrientation.Horizontal;

        private int gliphSize = 16;
        private bool tree;
        private int headerWidth = 40;

        public LayoutListInfo()
        {
            columns = new LayoutColumnMap(this);
            sorters = new LayoutSortList(this);
        }

        public LayoutListInfo(params LayoutColumn[] items) : this()
        {
            columns.AddRange(items);
        }

        public LayoutListInfo(IEnumerable<LayoutColumn> items, IEnumerable<LayoutSort> sorts) : this()
        {
            columns.AddRange(items);
            if (sorts != null)
                sorters.AddRange(sorts);
        }

        public LayoutListInfoKeys Keys
        {
            get { return keys; }
            set { keys = value; }
        }

        [XmlIgnore, Browsable(false)]
        public bool GridMode
        {
            get { return (keys & LayoutListInfoKeys.GridMode) == LayoutListInfoKeys.GridMode; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.GridMode;
                else
                    keys &= ~LayoutListInfoKeys.GridMode;
                OnBoundChanged(EventArgs.Empty);
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool CollectingRow
        {
            get { return (keys & LayoutListInfoKeys.CollectingRow) == LayoutListInfoKeys.CollectingRow; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.CollectingRow;
                else
                    keys &= ~LayoutListInfoKeys.CollectingRow;
                OnBoundChanged(EventArgs.Empty);
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool HeaderVisible
        {
            get { return (keys & LayoutListInfoKeys.HeaderVisible) == LayoutListInfoKeys.HeaderVisible; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.HeaderVisible;
                else
                    keys &= ~LayoutListInfoKeys.HeaderVisible;
                OnBoundChanged(EventArgs.Empty);
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool HeaderFrosen
        {
            get { return (keys & LayoutListInfoKeys.HeaderFrosen) == LayoutListInfoKeys.HeaderFrosen; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.HeaderFrosen;
                else
                    keys &= ~LayoutListInfoKeys.HeaderFrosen;
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool ColumnsVisible
        {
            get { return (keys & LayoutListInfoKeys.ColumnsVisible) == LayoutListInfoKeys.ColumnsVisible; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.ColumnsVisible;
                else
                    keys &= ~LayoutListInfoKeys.ColumnsVisible;
                OnBoundChanged(EventArgs.Empty);
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool HotTrackingRow
        {
            get { return (keys & LayoutListInfoKeys.HotTrackingRow) == LayoutListInfoKeys.HotTrackingRow; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.HotTrackingRow;
                else
                    keys &= ~LayoutListInfoKeys.HotTrackingRow;
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool HotTrackingCell
        {
            get { return (keys & LayoutListInfoKeys.HotTrackingCell) == LayoutListInfoKeys.HotTrackingCell; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.HotTrackingCell;
                else
                    keys &= ~LayoutListInfoKeys.HotTrackingCell;
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool HotSelection
        {
            get { return (keys & LayoutListInfoKeys.HotSelectRow) == LayoutListInfoKeys.HotSelectRow; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.HotSelectRow;
                else
                    keys &= ~LayoutListInfoKeys.HotSelectRow;
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool ShowToolTip
        {
            get { return (keys & LayoutListInfoKeys.ShowToolTip) == LayoutListInfoKeys.ShowToolTip; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.ShowToolTip;
                else
                    keys &= ~LayoutListInfoKeys.ShowToolTip;
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool GroupName
        {
            get { return (keys & LayoutListInfoKeys.GroupName) == LayoutListInfoKeys.GroupName; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.GroupName;
                else
                    keys &= ~LayoutListInfoKeys.GroupName;
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool GroupCount
        {
            get { return (keys & LayoutListInfoKeys.GroupCount) == LayoutListInfoKeys.GroupCount; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.GroupCount;
                else
                    keys &= ~LayoutListInfoKeys.GroupCount;
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool GridAuto
        {
            get { return (keys & LayoutListInfoKeys.GridAuto) == LayoutListInfoKeys.GridAuto; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.GridAuto;
                else
                    keys &= ~LayoutListInfoKeys.GridAuto;
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool CalcHeigh
        {
            get { return (keys & LayoutListInfoKeys.CalcHeigh) == LayoutListInfoKeys.CalcHeigh; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.CalcHeigh;
                else
                    keys &= ~LayoutListInfoKeys.CalcHeigh;
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool CalcWidth
        {
            get { return (keys & LayoutListInfoKeys.CalcWidth) == LayoutListInfoKeys.CalcWidth; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.CalcWidth;
                else
                    keys &= ~LayoutListInfoKeys.CalcWidth;
            }
        }

        [XmlIgnore, Browsable(false)]
        public bool GroupHeader
        {
            get { return (keys & LayoutListInfoKeys.GroupHeader) == LayoutListInfoKeys.GroupHeader; }
            set
            {
                if (value)
                    keys |= LayoutListInfoKeys.GroupHeader;
                else
                    keys &= ~LayoutListInfoKeys.GroupHeader;
            }
        }

        //[DefaultValue(GridOrientation.Horizontal)]
        public GridOrientation GridOrientation
        {
            get { return gridOrient; }
            set
            {
                if (gridOrient != value)
                {
                    gridOrient = value;
                    OnBoundChanged(EventArgs.Empty);
                }
            }
        }

        [DefaultValue(1)]
        public int GridCol
        {
            get { return gridCol; }
            set
            {
                if (gridCol != value)
                {
                    gridCol = value;
                    OnBoundChanged(EventArgs.Empty);
                }
            }
        }

        public int GliphSize
        {
            get { return gliphSize; }
            set { gliphSize = value; }
        }

        [DefaultValue(10)]
        public int GroupIndent { get; set; } = 10;

        [DefaultValue(1D)]
        public double Scale
        {
            get { return scale; }
            set
            {
                if (scale == value)
                    return;
                scale = value;
                OnBoundChanged(EventArgs.Empty);
            }
        }

        [DefaultValue(0)]
        public int Indent { get; set; }

        [DefaultValue(19)]
        public int LevelIndent { get; set; } = 19;

        [Browsable(false), DefaultValue("Group")]
        public string StyleGroupName { get; set; } = "Group";

        [Browsable(false), DefaultValue("Row")]
        public string StyleRowName { get; set; } = "Row";

        [Browsable(false), DefaultValue("Cell")]
        public string StyleCellName { get; set; } = "Cell";

        [Browsable(false), DefaultValue("Column")]
        public string StyleColumnName { get; set; } = "Column";

        [Browsable(false), DefaultValue("Header")]
        public string StyleHeaderName { get; set; } = "Header";

        [XmlIgnore]
        public CellStyle StyleCell
        {
            get { return this.styleCell ?? (styleCell = GuiEnvironment.StylesInfo[StyleCellName]); }
            set
            {
                styleCell = value;
                StyleCellName = value?.Name;
            }
        }

        [XmlIgnore]
        public CellStyle StyleRow
        {
            get { return styleRow ?? (styleRow = GuiEnvironment.StylesInfo[StyleRowName]); }
            set
            {
                styleRow = value;
                StyleRowName = value?.Name;
            }
        }

        [XmlIgnore]
        public CellStyle StyleColumn
        {
            get { return styleColumn ?? (styleColumn = GuiEnvironment.StylesInfo[StyleColumnName]); }
            set
            {
                styleColumn = value;
                StyleColumnName = value?.Name;
            }
        }

        [XmlIgnore]
        public CellStyle StyleGroup
        {
            get { return styleGroup ?? (styleGroup = GuiEnvironment.StylesInfo[StyleGroupName]); }
            set
            {
                styleGroup = value;
                StyleGroupName = value?.Name;
            }
        }

        [XmlIgnore]
        public CellStyle StyleHeader
        {
            get { return styleHeader ?? (styleHeader = GuiEnvironment.StylesInfo[StyleHeaderName]); }
            set
            {
                styleHeader = value;
                StyleHeaderName = value?.Name;
            }
        }

        public event EventHandler BoundChanged;

        public virtual void OnBoundChanged(EventArgs e)
        {
            columns.Bound = Rectangle.Zero;
            foreach (var item in LayoutMapHelper.GetItems(columns))
            {
                item.Bound = Rectangle.Zero;
            }
            BoundChanged?.Invoke(this, e);
        }

        public string Filter
        {
            get { return filter; }
            set { filter = value; }
        }

        public bool Tree
        {
            get { return tree; }
            set { tree = value; }
        }

        public LayoutSortList Sorters
        {
            get { return sorters; }
            set
            {
                value.Info = this;
                sorters = value;
            }
        }

        public LayoutColumnMap Columns
        {
            get { return columns; }
            set
            {
                value.Info = this;
                columns = value;
            }
        }

        [DefaultValue(40)]
        public int HeaderWidth
        {
            get { return HeaderVisible ? headerWidth : 0; }
            set
            {
                if (headerWidth == value)
                    return;
                headerWidth = value;
                if (headerWidth < 20)
                    headerWidth = 20;
                OnBoundChanged(EventArgs.Empty);
            }
        }

        public bool GroupVisible
        {
            get
            {
                if (sorters != null)
                    foreach (LayoutSort sort in Sorters)
                        if (sort.IsGroup)
                            return true;
                return false;
            }
        }

        [DefaultValue(20)]
        public int GroupHeigh { get; set; } = 20;

        public void ResetGroup()
        {
            if (sorters != null)
                foreach (LayoutSort sort in Sorters)
                    sort.IsGroup = false;
        }

        public void GetColumnsBound(double w, Func<ILayoutItem, double> wd, Func<ILayoutItem, double> hd)
        {
            if (Columns.Bound.Width.Equals(0) || CalcWidth || CalcHeigh || Columns.FillWidth)
            {
                LayoutMapHelper.GetBound(columns, HeaderVisible ? w - headerWidth * scale : w, 0, wd, hd);

                Columns.Bound = new Rectangle(Columns.Bound.X + HeaderWidth,
                                             Columns.Bound.Y,
                                             Columns.Bound.Width,
                                             Columns.Bound.Height);
            }
        }

        public void GetBound(ILayoutItem column, Func<ILayoutItem, double> wd, Func<ILayoutItem, double> hd)
        {
            if (column.Bound.Width.Equals(0) || CalcWidth || CalcHeigh || columns.FillWidth)
            {
                LayoutMapHelper.GetBound(columns, column, wd, hd);
            }
        }

        public IEnumerable<LayoutColumn> GetDisplayed(double left, double right)
        {
            foreach (ILayoutItem column in LayoutMapHelper.GetVisibleItems(columns))
            {
                GetBound(column, null, null);
                if (column.Bound.Right > left && column.Bound.Left < right)
                {
                    yield return (LayoutColumn)column;
                }
            }
        }

        public void ScaleRec(ref Rectangle rec)
        {
            if (!scale.Equals(1D))
            {
                rec.X *= scale;
                rec.Y *= scale;
                rec.Width *= scale;
                rec.Height *= scale;
            }
        }

        public void Dispose()
        {
            var list = LayoutMapHelper.GetItems(columns);
            foreach (LayoutColumn col in list)
                col.Dispose();
        }
    }

    public class StyleComparer
    {
        public Action<object> Checker;
        public CellStyle Style;

        public StyleComparer(Action<object> checker, CellStyle style)
        {
            this.Checker = checker;
            this.Style = style;
        }
    }
}
