using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class LayoutItem<T> : NamedList<T>, ILayoutItem, IComparable where T : LayoutItem<T>, new()
    {
        private static readonly double min = 5;
        protected internal double height = 22D;
        protected internal double width = 120D;
        protected int row;
        protected int col;
        protected bool visible = true;
        protected bool fillW;
        protected bool fillH;
        protected string name;
        protected object tag;
        protected Rectangle bound;
        public Func<ILayoutItem, double> CalcHeight;
        public Func<ILayoutItem, double> CalcWidth;
        private INotifyListChanged container;
        private double scale = 1;
        private double indent;

        public LayoutItem() : base()
        {
            ApplySortInternal(new LayoutItemComparer<T>());
        }

        [XmlIgnore]
        public virtual Rectangle Bound
        {
            get { return bound; }
            set { bound = value; }
        }

        [DefaultValue(1D)]
        public virtual double Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        [DefaultValue(LayoutGrowMode.Horizontal)]
        public LayoutGrowMode GrowMode { get; set; }

        public T this[int rowIndex, int colIndex]
        {
            get { return Get(rowIndex, colIndex); }
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (name == value)
                    return;
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public int Row
        {
            get { return row; }
            set
            {
                if (row == value)
                    return;
                row = value;
                OnPropertyChanged(nameof(Row));
            }
        }

        public int Col
        {
            get { return col; }
            set
            {
                if (col == value)
                    return;
                col = value;
                OnPropertyChanged(nameof(Col));
            }
        }

        [DefaultValue(22D)]
        public double Height
        {
            get { return Count > 0 ? GetHeight(height, CalcHeight) : height; }
            set
            {
                if (value.Equals(height) || value < min)
                    return;
                height = value;
                OnPropertyChanged(nameof(Height));
            }
        }

        [DefaultValue(120D)]
        public double Width
        {
            get { return Count > 0 ? GetWidth(width, CalcWidth) : width; }
            set
            {
                if (value.Equals(width) || value < min)
                    return;
                width = value;
                OnPropertyChanged(nameof(Width));
            }
        }

        [DefaultValue(true)]
        public virtual bool Visible
        {
            get { return visible || IsVisible(); }
            set
            {
                if (visible == value)
                    return;
                visible = value;
                OnPropertyChanged(nameof(Visible));
            }
        }

        [DefaultValue(false)]
        public virtual bool FillWidth
        {
            get { return fillW || IsFillWidth(); }
            set
            {
                if (fillW == value)
                    return;
                fillW = value;
                OnPropertyChanged(nameof(FillWidth));
            }
        }

        [DefaultValue(false)]
        public virtual bool FillHeight
        {
            get { return fillH || IsFillHeight(); }
            set
            {
                if (fillH == value)
                    return;
                fillH = value;
                OnPropertyChanged(nameof(FillHeight));
            }
        }

        [XmlIgnore]
        public T TopMap
        {
            get
            {
                T temp = (T)this;
                while (temp.Map != null)
                {
                    temp = temp.Map;
                }
                return temp;
            }
        }

        ILayoutItem ILayoutItem.Map
        {
            get { return Map; }
            //set { Map = (T)value; }
        }

        [XmlIgnore]
        public T Map
        {
            get { return (T)Container; }
        }

        [XmlIgnore, Browsable(false)]
        public INotifyListChanged Container
        {
            get { return container; }
            set
            {
                container = value;
                //OnPropertyChanged(nameof(Map));
            }
        }

        [XmlIgnore]
        public object Tag
        {
            get { return tag; }
            set
            {
                if (tag == value)
                    return;
                tag = value;
                OnPropertyChanged(nameof(Tag));
            }
        }

        [DefaultValue(0D)]
        public virtual double Indent
        {
            get { return indent; }
            set
            {
                if (indent != value)
                {
                    indent = value;
                    OnPropertyChanged(nameof(Indent));
                }
            }
        }

        public void Remove()
        {
            Map?.Remove(this);
        }

        public override string ToString()
        {
            return string.Format("({0},{1}) {2}", row, col, name);
        }

        #region IComparable implementation

        public int CompareTo(object obj)
        {
            return Compare(this, obj as ILayoutItem);
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string property)
        {
            bound.Width = 0D;
            var args = new PropertyChangedEventArgs(property);
            PropertyChanged?.Invoke(this, args);
            Container?.OnPropertyChanged(this, args);
        }

        public bool Contains(string property)
        {
            return GetRecursive(property) != null;
        }

        public override void InsertInternal(int index, T item)
        {
            if (string.IsNullOrEmpty(item.Name))
                item.Name = $"item_{item.Col}_{item.Row}";
            base.InsertInternal(index, item);
        }

        public override int AddInternal(T item)
        {
            if (Contains(item))
                return IndexOf(item);
            if (item.Col == 0 && item.Row == 0)
            {
                if (GrowMode == LayoutGrowMode.Vertical)
                {
                    item.Row = GetRowMaxIndex() + 1;
                }
                if (item.Col == 0)
                {
                    item.Col = GetRowColumnCount(item.Row);
                }
            }
            return base.AddInternal(item);
        }

        public void InsertCol(int index, T item)
        {
            item.Col = index;
            Map.Insert(item, false);
        }

        public void InsertRow(int index, T item)
        {
            item.Row = index;
            Map.Insert(item, true);
        }

        public virtual void InsertBefore(T column)
        {
            column.Row = Row;
            column.Col = Col;
            Map.Insert(column, false);
        }

        public virtual void InsertBefore(IEnumerable<T> columns)
        {
            var row = Row;
            var col = Col;
            foreach (var column in columns)
            {
                column.Row = row;
                column.Col = col;
                Map.Insert(column, GrowMode == LayoutGrowMode.Vertical);
                if (GrowMode == LayoutGrowMode.Horizontal)
                {
                    col++;
                }
                else
                {
                    row++;
                }
            }
        }

        public virtual void InsertAfter(T column)
        {
            column.Row = Row;
            column.Col = Col + 1;
            Map.Insert(column, false);
        }

        public virtual void InsertAfter(IEnumerable<T> columns)
        {
            var row = Row;
            var col = Col;
            foreach (var column in columns)
            {
                if (GrowMode == LayoutGrowMode.Horizontal)
                {
                    col++;
                }
                else
                {
                    row++;
                }

                column.Row = row;
                column.Col = col;
                Map.Insert(column, GrowMode == LayoutGrowMode.Vertical);
            }
        }

        public void Insert(T item, bool inserRow)
        {
            item.Remove();
            if (inserRow)
                item.Col = 0;
            var exs = Get(item.Row, item.Col);
            if (exs != null)
            {
                var buffer = inserRow
                ? ((IEnumerable<T>)this).Where(p => item.Row <= p.Row && item.Col <= p.Col).ToArray()
                : ((IEnumerable<T>)this).Where(p => item.Row == p.Row && item.Col <= p.Col).ToArray();
                foreach (var col in buffer)
                {
                    if (inserRow)
                        col.Row++;
                    else
                        col.Col++;
                }
            }
            if (string.IsNullOrEmpty(item.Name))
            {
                item.Name = "litem" + items.Count;
            }
            Insert(GetIndexBySort(item), item);
        }

        public void InsertWith(T newItem, LayoutAlignType type, bool grouping)
        {
            if (grouping)
            {
                Grouping(newItem, type);
            }
            else
            {
                newItem.Row = Row;
                newItem.Col = Col;
                //move only by change indexes
                bool inserRow = false;
                if (type == LayoutAlignType.Right)
                    newItem.Col++;
                else if (type == LayoutAlignType.Top)
                    inserRow = true;
                else if (type == LayoutAlignType.Bottom)
                {
                    inserRow = true;
                    newItem.Row++;
                }
                Map.Insert(newItem, inserRow);
            }
        }

        public void Move(T moved, LayoutAlignType type, bool builGroup)
        {
            //check collision 
            //if (Contains(moved.Map, destination) && moved.Map != destination.Map && moved.Map.Map != null)
            //    return;

            if (moved.Map == Map && Map.Map != null)
            {
                builGroup = false;
            }

            //remove from old map 
            Remove(moved);

            if (Map.Count == 1)
            {
                builGroup = false;
            }

            InsertWith(moved, type, builGroup);
        }

        public void Grouping(T newItem, LayoutAlignType anch)
        {
            T map = new T();
            Replace(map);

            Row = 0;
            Col = 0;
            newItem.Row = 0;
            newItem.Col = 0;

            if (anch == LayoutAlignType.Top)
                Row = 1;
            else if (anch == LayoutAlignType.Bottom)
                newItem.Row = 1;
            else if (anch == LayoutAlignType.Right)
                Col = 1;
            else if (anch == LayoutAlignType.Left)
                newItem.Col = 1;

            map.Add((T)this);
            map.Add(newItem);
        }


        public override bool Remove(T item)
        {
            if (item.Map == null)
                return false;
            if (item.Map != this)
                return item.Map.Remove(item);
            var buffer = ((IEnumerable<T>)this).Where(p => (p.Row == item.Row && p.Col > item.Col) || p.Row > item.Row).ToArray();
            var removeRow = !((IEnumerable<T>)this).Select(p => p.Row == item.Row).Any();
            base.Remove(item);
            foreach (var element in buffer)
            {
                if (element.Row == item.Row)
                    element.Col--;
                else if (removeRow && element.Row > item.Row)
                    element.Row--;
            }

            if (Map != null)
            {
                if (Count == 1)
                {
                    this[0].Replace((T)this);//map.Map, 
                }
                else if (Count == 0)
                {
                    Remove();
                }
            }
            return true;
        }

        public void Replace(T newColumn)
        {
            var tempMap = Map;
            int index = tempMap.IndexOf(this);
            newColumn.Row = Row;
            newColumn.Col = Col;
            tempMap.RemoveInternal((T)this, index);
            tempMap.InsertInternal(index, newColumn);
        }

        public override void OnListChanged(ListChangedType type, int newIndex = -1, int oldIndex = -1, string property = null)
        {
            base.OnListChanged(type, newIndex, oldIndex, property);
            if (Map != null)
            {
                Map.OnListChanged(type, newIndex, oldIndex, property);
            }
        }

        public virtual Rectangle GetBound()
        {
            return GetBound(width, height, CalcWidth, CalcHeight);
        }

        public virtual Rectangle GetBound(double maxWidth, double maxHeight)
        {
            return GetBound(maxWidth, maxHeight, CalcWidth, CalcHeight);
        }

        public virtual Rectangle GetBound(double maxWidth, double maxHeight, Func<ILayoutItem, double> calcWidth, Func<ILayoutItem, double> calcHeight)
        {
            return Bound = new Rectangle(0, 0, GetWidth(maxWidth + Indent, calcWidth), GetHeight(maxHeight + Indent, calcHeight)).Inflate(Indent, Indent);
        }

        public virtual Rectangle GetBound(T item)
        {
            GetBound(item, CalcWidth, CalcHeight);
            return item.Bound;
        }

        public void GetBound(T item, Func<ILayoutItem, double> calcWidth, Func<ILayoutItem, double> calcHeight)
        {
            double x = 0, y = 0;
            int r = -1;
            T subMap = null;
            var bound = new Rectangle();
            foreach (var entry in this)
            {
                if (entry.Row != r)
                {
                    x = 0;
                    if (r != -1)
                        y += GetRowHeight(r, Bound.Height, true, calcHeight);
                    r = entry.Row;
                    //if (col.Row < column.Row)
                    //     continue;
                }
                if (!entry.Visible)
                    continue;
                if (entry == item)
                {
                    bound.X = x;
                    bound.Y = y;
                    bound.Width = entry.GetItemWidth(Bound.Width, calcWidth);
                    bound.Height = GetRowHeight(r, Bound.Height, true, calcHeight);
                    break;
                }
                else if (entry.Contains(item))
                {
                    subMap = entry;
                    GetBound(subMap, calcWidth, calcHeight);
                    subMap.GetBound(item, calcWidth, calcHeight);
                    return;
                }
                x += entry.GetItemWidth(Bound.Width, calcWidth);
            }
            if (item.Map == this)
            {
                bound.X += Bound.X;// + imap.Indent
                bound.Y += Bound.Y;// + imap.Indent

                if (item.IsLastCol() && bound.Right < Bound.Right)
                    bound.Width += Bound.Right - bound.Right - Indent;
                if (item.IsLastRow() && bound.Bottom < Bound.Bottom)
                    bound.Height += Bound.Bottom - bound.Bottom - Indent;
            }
            item.Bound = bound;
        }

        public bool IsFillWidth()
        {
            foreach (var item in this)
            {
                if (item.Visible && item.FillWidth)
                    return true;
            }
            return false;
        }

        public bool IsFillHeight()
        {
            foreach (var item in this)
            {
                if (item.Visible && item.FillHeight)
                    return true;
            }
            return false;
        }

        public void SetFillWidth(bool value)
        {
            foreach (var item in this)
            {
                item.FillWidth = value;
            }
        }

        public void SetFillHeight(bool value)
        {
            foreach (var item in this)
            {
                item.FillHeight = value;
            }
        }

        public bool IsVisible()
        {
            foreach (ILayoutItem col in this)
            {
                if (col.Visible)
                    return true;
            }
            return false;
        }

        public bool IsLastCol()
        {
            if (Map == null)
                return false;

            foreach (var entry in Map)
            {
                if (entry.Row == Row)
                {
                    if (entry.Visible && entry.Col > Col)
                        return false;
                }
                else if (entry.Row > Row)
                    return true;
            }
            return true;
        }

        public bool IsLastRow()
        {
            if (Map == null)
                return false;
            return Map.GetRowMaxIndex() <= Row;
        }

        public int GetRowHeightSpan(int rowIndex, bool requrcy)
        {
            int h = 0;
            int hh = 0;
            foreach (var item in this)
            {
                if (item.Visible && item.Row == rowIndex)
                {
                    if (item.Count > 0)
                    {
                        hh = item.GetHeightSpan();
                        if (hh > h)
                            h = hh;
                    }
                    else if (h == 0)
                        h = 1;
                }
                else if (item.Row > rowIndex)
                    break;
            }
            if (Map != null && requrcy)
            {
                hh = ((T)Map).GetRowHeightSpan(Row, requrcy);
                int r = GetRowMaxIndex();
                if (hh > h && r == rowIndex && r < hh - 1)
                    h = hh - h;
            }
            return h;
        }

        public double GetRowHeight(int rowIndex, double max, bool calcFill, Func<ILayoutItem, double> calc)
        {
            double h = 0;
            foreach (var item in this)
            {
                if (item.Visible && item.Row == rowIndex)
                {
                    double hh = 0;
                    if (!item.FillHeight || calcFill)//|| max<=0 
                    {
                        hh = item.GetItemHeight(max, calc);
                    }
                    else
                    {
                        h = 0;
                        break;
                    }
                    if (hh > h)
                        h = hh;
                }
                else if (item.Row > rowIndex)
                    break;
            }
            return h;
        }

        public int GetRowWidthSpan(int row)
        {
            int w = 0;
            foreach (T item in this)
            {
                if (item.Row == row)
                {
                    if (item.Visible)
                    {
                        if (item.Count > 0)
                            w += item.GetWithdSpan();
                        else
                            w++;
                    }
                }
                else if (item.Row > row)
                    break;
            }
            return w;
        }

        public double GetRowWidth(int row, double max, Func<ILayoutItem, double> calc)
        {
            double w = 0;
            foreach (var item in this)
            {
                if (item.Row == row && item.Visible)
                {
                    if (!item.FillWidth || (max <= 0 && calc != null))
                        w += item.GetItemWidth(max, calc);
                }
                else if (item.Row > row)
                {
                    break;
                }
            }
            return w;
        }

        public int GetRowMaxIndex()
        {
            return Count > 0 ? this[Count - 1].Row : -1;
        }

        public int GetHeightSpan()
        {
            int h = 0;
            int r = -1;
            int max = GetRowMaxIndex();
            foreach (var item in this)
            {
                if (item.Row != r)
                {
                    r = item.Row;
                    h += GetRowHeightSpan(r, false);
                    if (r == max)
                        break;
                }
            }
            return h;
        }

        public double GetHeight(double max, Func<ILayoutItem, double> calc)
        {
            if (FillHeight && max > 0)
                return max;
            double h = 0;
            int row = -1;
            int rmax = GetRowMaxIndex();
            foreach (var item in this)
            {
                if (item.Row != row)
                {
                    row = item.Row;
                    h += GetRowHeight(row, max, false, calc);
                    if (row == rmax)
                        break;
                }
            }
            return h;
        }

        public double GetItemHeight(double max, Func<ILayoutItem, double> calc)
        {
            double height = 0;
            if (FillHeight && max > 0)
            {
                double itemsH = Map.GetHeight(0D, calc);
                double itemH = max - itemsH;
                itemH = itemH < 30 ? 30 : itemH;
                int c = 1;
                int r = Row;
                foreach (var sitem in Map)
                {
                    if (sitem.Visible && sitem.FillHeight && sitem.Row != r && sitem.Row != Row)
                    {
                        r = sitem.Row;
                        c++;
                    }
                }
                height = itemH / c;
            }
            else
            {
                height = calc == null ? Height : calc(this);
                if (Map != null)
                    height *= TopMap.Scale;
            }
            return height;
        }

        public int GetWithdSpan()
        {
            int w = 0;
            int r = -1;
            var max = GetRowMaxIndex();
            foreach (var col in this)
            {
                if (col.Row != r)
                {
                    r = col.Row;
                    int ww = GetRowWidthSpan(r);
                    if (ww > w)
                        w = ww;
                }
                if (r == max)
                    break;
            }
            return w;
        }

        public double GetItemWidth(double max, Func<ILayoutItem, double> calc)
        {
            double width = 0;
            if (FillWidth && max > 0)
            {
                double itemsW = Map.GetWidth(0D, calc);
                double itemW = max - itemsW;
                itemW = itemW < 30 ? 30 : itemW;
                int c = 0;
                foreach (var sitem in Map)
                    if (sitem.Visible && sitem.FillWidth && sitem.Row == Row)
                        c++;
                width = itemW / c;
            }
            else
            {
                width = calc == null ? Width : calc(this);
                if (Map != null)
                    width *= TopMap.Scale;
            }

            return width;
        }

        public double GetWidth(double max, Func<ILayoutItem, double> calc)
        {
            if (FillWidth && max > 0)
                return max;
            double w = 0;
            int r = -1;
            bool fill = false;
            foreach (var item in this)
            {
                if (item.Row != r)
                {
                    r = item.Row;
                    double ww = GetRowWidth(r, max, calc);
                    if (ww > w && !fill)
                        w = ww;
                }
                if (!fill && item.Visible && item.FillWidth)
                    fill = true;
                if (r == GetRowMaxIndex())
                    break;
            }
            return w;
        }

        public IEnumerable<T> GetItems()
        {
            foreach (var item in this)
            {
                if (item.Count > 0)
                {
                    foreach (var subItem in item)
                        yield return subItem;
                }
                else
                {
                    yield return item;
                }
            }
        }

        public IEnumerable<T> GetVisibleItems()
        {
            foreach (var item in GetItems())
            {
                if (item.Visible)
                    yield return item;
            }
        }

        public void Reset()
        {
            var list = GetItems().ToArray();
            Clear();
            foreach (var col in list)
            {
                Add(col);
            }
        }

        public T Get(int row, int col)
        {
            foreach (var item in this)
            {
                if (item.Row == row && item.Col == col)
                    return item;
            }
            return null;
        }

        public T GetRecursive(string name)
        {
            foreach (var item in this)
            {
                if (item.Name == name)
                {
                    return item;
                }
                if (item.Count > 0)
                {
                    var c = item.GetRecursive(name);
                    if (c != null)
                        return c;
                }
            }
            return null;
        }

        public override bool Contains(T item)
        {
            var temp = item.Map;
            while (temp != null)
            {
                if (temp == this)
                    return true;
                temp = temp.Map;
            }
            return false;
        }

        public int GetRowColumnCount(int index)
        {
            int i = 0;
            foreach (var item in this)
            {
                if (item.Row == index)
                    i++;
                else if (item.Row > index)
                    break;
            }
            return i;
        }

        public void GetVisibleIndex(T item, out int c, out int r)
        {
            //Interval<double> rez = new Interval<double>();
            c = -1;
            r = -1;
            int tr = -100;
            foreach (var entry in this)
            {
                if (entry.Visible)
                {
                    int sc = 0, sr = 0;

                    if (tr != entry.Row)
                    {
                        tr = entry.Row;
                        c = 0;
                        r++;
                    }
                    else
                        c++;
                    if (entry.Count > 0 && entry != item)
                    {
                        entry.GetVisibleIndex(null, out sc, out sr);
                        if (item == null || entry.Col < item.Col)
                            c += sc;
                        if (item == null || entry.Row < item.Row)
                            r += sr;
                    }
                    if (entry == item)
                        break;
                }
            }
        }

        public static int Compare(ILayoutItem x, ILayoutItem y)
        {
            int rez = 0;
            if (x == null && y == null)
                rez = 0;
            else if (x == null)
                rez = -1;
            else if (y == null)
                rez = 1;
            else
            {
                if (x.Map != null && y.Map != null)
                    rez = Compare(x.Map, y.Map);
                if (rez == 0)
                    rez = x.Row.CompareTo(y.Row);
                if (rez == 0)
                    rez = x.Col.CompareTo(y.Col);
            }
            return rez;
        }

        public bool IsFirstItem()
        {
            foreach (var entry in Map)
            {
                if (entry.Row == Row)
                {
                    if (entry.Col < Col)
                        return false;
                    else// if (col.Col > column.Col)
                        break;
                }
            }
            return true;
        }

        IEnumerator<ILayoutItem> IEnumerable<ILayoutItem>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }

    public enum LayoutGrowMode
    {
        Horizontal,
        Vertical
    }

    public enum LayoutAlignType
    {
        None,
        Left,
        Right,
        Top,
        Bottom
    }
}

