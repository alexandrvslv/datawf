using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Xwt;

namespace DataWF.Gui
{
    public class LayoutItem<T> : NamedList<T>, ILayoutItem, IGroup, IComparable where T : LayoutItem<T>, new()
    {
        private static readonly double min = 5;
        protected internal double height = 22D;
        protected internal double width = 120D;
        protected int row = -1;
        protected int column = -1;
        protected bool visible = true;
        protected bool fillW;
        protected bool fillH;
        protected string name;
        protected object tag;
        //protected Rectangle bound;
        public Func<ILayoutItem, double> CalcHeight;
        public Func<ILayoutItem, double> CalcWidth;
        private INotifyListPropertyChanged container;
        private double scale = 1;
        private double indent;
        private bool expand = true;

        public LayoutItem() : base()
        {
            ApplySortInternal(new LayoutItemComparer<T>());
        }

        //[XmlIgnore]
        //public virtual Rectangle Bound
        //{
        //    get { return bound; }
        //    set { bound = value; }
        //}

        [DefaultValue(1D)]
        public virtual double Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        [DefaultValue(Orientation.Horizontal)]
        public Orientation GrowMode { get; set; }

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
                OnPropertyChanged();
            }
        }

        [DefaultValue(-1)]
        public int Row
        {
            get { return row; }
            set
            {
                if (row == value)
                    return;
                row = value;
                OnPropertyChanged();
            }
        }

        [DefaultValue(-1)]
        public int Column
        {
            get { return column; }
            set
            {
                if (column == value)
                    return;
                column = value;
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
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
                OnPropertyChanged();
            }
        }

        //[DefaultValue(false)]
        public virtual bool FillWidth
        {
            get { return Count > 0 ? IsFillWidth() : fillW; }
            set
            {
                if (fillW == value)
                    return;

                fillW = value;

                OnPropertyChanged();
            }
        }

        //[DefaultValue(false)]
        public virtual bool FillHeight
        {
            get { return Count > 0 ? IsFillHeight() : fillH; }
            set
            {
                if (fillH == value)
                    return;

                fillH = value;

                OnPropertyChanged();
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
            get { return (T)Containers.FirstOrDefault(); }
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
                OnPropertyChanged();
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
                    OnPropertyChanged();
                }
            }
        }

        [Browsable(false)]
        public bool IsExpanded { get { return GroupHelper.GetAllParentExpand(this); } }

        [XmlIgnore, Browsable(false)]
        public IGroup Group
        {
            get { return Map; }
            set
            {
                if (value != null)
                    ((T)value).Add((T)this);
                else
                    Remove();
            }
        }

        [XmlIgnore, DefaultValue(true)]
        public virtual bool Expand
        {
            get { return expand; }
            set
            {
                if (expand == value)
                    return;
                expand = value;
                OnPropertyChanged();
            }
        }

        [Browsable(false)]
        public bool IsCompaund { get { return items.Count > 0; } }

        public IEnumerable<IGroup> GetGroups()
        {
            return this;
        }

        public void Remove()
        {
            Map?.Remove(this);
        }

        public override string ToString()
        {
            return string.Format("({0},{1}) {2}", row, column, name);
        }

        #region IComparable implementation

        public int CompareTo(object obj)
        {
            return Compare(this, obj as ILayoutItem);
        }

        #endregion

        public bool Contains(string property)
        {
            return GetRecursive(property) != null;
        }

        public override void InsertInternal(int index, T item)
        {
            if (item == this)
                throw new InvalidOperationException("Layout self reference!");
            if (string.IsNullOrEmpty(item.Name))
                item.Name = $"item_{item.Column}_{item.Row}";
            base.InsertInternal(index, item);
        }

        public override int AddInternal(T item)
        {
            if (Contains(item))
                return -IndexOf(item);
            if (item.Row < 0)
            {
                item.Row = GrowMode == Orientation.Vertical
                    ? GetRowMaxIndex() + 1 : 0;
            }
            if (item.Column < 0)
            {
                item.Column = GetRowColumnCount(item.Row);
            }
            return base.AddInternal(item);
        }

        public void InsertCol(int index, T item)
        {
            item.Column = index;
            Map.Insert(item, false);
        }

        public virtual void ApplyBound(Rectangle value)
        {
            //throw new NotImplementedException();
        }

        public void InsertRow(int index, T item)
        {
            item.Row = index;
            Map.Insert(item, true);
        }

        public virtual void InsertBefore(T item)
        {
            InsertBefore(new[] { item });
        }

        public virtual void InsertBefore(IEnumerable<T> items)
        {
            var row = Row;
            var column = Column;
            foreach (var item in items)
            {
                item.Row = row;
                item.Column = column;
                Map.Insert(item, Map.GrowMode == Orientation.Vertical);
                if (GrowMode == Orientation.Horizontal)
                {
                    column++;
                }
                else
                {
                    row++;
                }
            }
        }

        public virtual void InsertAfter(T item)
        {
            InsertAfter(new[] { item });
        }

        public virtual void InsertAfter(IEnumerable<T> items)
        {
            var row = Row;
            var column = Column;
            foreach (var item in items)
            {
                if (Map.GrowMode == Orientation.Horizontal)
                {
                    column++;
                }
                else
                {
                    row++;
                }

                item.Row = row;
                item.Column = column;
                Map.Insert(item, Map.GrowMode == Orientation.Vertical);
            }
        }

        public void Insert(T item, bool inserRow)
        {
            item.Remove();
            if (inserRow)
                item.Column = 0;
            var exs = Get(item.Row, item.Column);
            if (exs != null)
            {
                var buffer = inserRow
                ? ((IEnumerable<T>)this).Where(p => item.Row <= p.Row && item.Column <= p.Column).ToArray()
                : ((IEnumerable<T>)this).Where(p => item.Row == p.Row && item.Column <= p.Column).ToArray();
                foreach (var col in buffer)
                {
                    if (inserRow)
                        col.Row++;
                    else
                        col.Column++;
                }
            }
            if (string.IsNullOrEmpty(item.Name))
            {
                item.Name = "litem" + items.Count;
            }
            var index = GetIndexBySort(item);
            if (index < 0)
            {
                index = -index - 1;
            }
            Insert(index, item);
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
                newItem.Column = Column;
                //move only by change indexes
                bool inserRow = false;
                if (type == LayoutAlignType.Right)
                    newItem.Column++;
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
            Column = 0;
            newItem.Row = 0;
            newItem.Column = 0;

            if (anch == LayoutAlignType.Top)
                Row = 1;
            else if (anch == LayoutAlignType.Bottom)
                newItem.Row = 1;
            else if (anch == LayoutAlignType.Right)
                newItem.Column = 1;
            else if (anch == LayoutAlignType.Left)
                Column = 1;

            map.Add((T)this);
            map.Add(newItem);
        }


        public override bool Remove(T item)
        {
            if (item.Map == null)
                return false;
            if (item.Map != this)
                return item.Map.Remove(item);
            var buffer = ((IEnumerable<T>)this).Where(p => (p.Row == item.Row && p.Column > item.Column) || p.Row > item.Row).ToArray();
            var removeRow = !((IEnumerable<T>)this).Select(p => p.Row == item.Row).Any();
            base.Remove(item);
            foreach (var element in buffer)
            {
                if (element.Row == item.Row)
                    element.Column--;
                else if (removeRow && element.Row > item.Row)
                    element.Row--;
            }

            if (Map != null)
            {
                if (Count == 1)
                {
                    Replace(this[0]);//map.Map, 
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
            tempMap.RemoveInternal((T)this, index);
            newColumn.Remove();
            newColumn.Row = Row;
            newColumn.Column = Column;
            tempMap.InsertInternal(index, newColumn);
        }

        //public override void OnListChanged(NotifyCollectionChangedAction type, object sender = null, int index = -1, string property = null)
        //{
        //    base.OnListChanged(type, sender, index, property);
        //    if (Map != null)
        //    {
        //        Map.OnListChanged(type, sender, index, property);
        //    }
        //}

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
            var bound = new Rectangle(0, 0, GetWidth(maxWidth - Indent * 2, calcWidth), GetHeight(maxHeight - Indent * 2, calcHeight));
            bound.X += Indent;
            bound.Y += Indent;
            bound.Width += Indent;
            bound.Height += Indent;
            return bound;
        }

        public virtual Rectangle GetBound(T item, Rectangle mapBound)
        {
            return GetBound(item, mapBound, CalcWidth, CalcHeight);
        }

        public Rectangle GetBound(T item, Rectangle mapBound, Func<ILayoutItem, double> calcWidth, Func<ILayoutItem, double> calcHeight)
        {
            double x = 0, y = 0;
            int r = -1;
            var bound = new Rectangle();
            foreach (var entry in this)
            {
                if (entry.Row != r)
                {
                    x = 0;
                    if (r != -1)
                        y += GetRowHeight(r, mapBound.Height, true, calcHeight);
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
                    bound.Width = entry.GetItemWidth(mapBound.Width, calcWidth);
                    bound.Height = GetRowHeight(r, mapBound.Height, true, calcHeight);
                    break;
                }
                else if (entry.Contains(item))
                {
                    var entryBound = GetBound(entry, mapBound, calcWidth, calcHeight);
                    return entry.GetBound(item, entryBound, calcWidth, calcHeight);
                }
                x += entry.GetItemWidth(mapBound.Width, calcWidth);
            }
            if (item.Map == this)
            {
                bound.X += mapBound.X;// + imap.Indent
                bound.Y += mapBound.Y;// + imap.Indent

                if (item.IsLastCol() && bound.Right < mapBound.Right)
                    bound.Width += mapBound.Right - bound.Right - Indent;
                if (item.IsLastRow() && bound.Bottom < mapBound.Bottom)
                    bound.Height += mapBound.Bottom - bound.Bottom - Indent;
            }
            item.ApplyBound(bound);
            return bound;
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
            foreach (var item in this)
            {
                if (item.Visible)
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
                    if (entry.Visible && entry.Column > Column)
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

        public IEnumerable<T> GetAllItems()
        {
            foreach (var item in this)
            {
                yield return item;
                if (item.IsCompaund)
                {
                    foreach (var subItem in item.GetAllItems())
                        yield return subItem;
                }
            }
        }

        public T GetItem(string name)
        {
            if (name == null)
            {
                return null;
            }

            var find = this[name];
            if (find != null)
            {
                return find;
            }

            foreach (var item in this)
            {
                if (item.IsCompaund)
                {
                    find = item.GetItem(name);
                    if (find != null)
                    {
                        return find;
                    }
                }
            }
            return null;
        }

        public IEnumerable<T> GetItems()
        {
            foreach (var item in this)
            {
                if (item.IsCompaund)
                {
                    foreach (var subItem in item.GetItems())
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

        public void ResetCollection()
        {
            var list = GetItems().ToArray();
            Clear();
            foreach (var col in list)
            {
                Add(col);
            }
        }

        public T Get(int row, int column)
        {
            foreach (var item in this)
            {
                if (item.Row == row && item.Column == column)
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
                        if (item == null || entry.Column < item.Column)
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
                    rez = x.Column.CompareTo(y.Column);
            }
            return rez;
        }

        public bool IsFirstItem()
        {
            foreach (var entry in Map)
            {
                if (entry.Row == Row)
                {
                    if (entry.Column < Column)
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
            if (items != null)
            {
                foreach (var item in items)
                {
                    item.Dispose();
                }
            }
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

