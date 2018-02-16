using System;
using System.ComponentModel;
using System.Collections.Generic;
using Xwt;
using System.Xml.Serialization;
using DataWF.Common;
using System.Collections;

namespace DataWF.Gui
{
    public class LayoutMap : ILayoutMap, IComparable, IComparable<LayoutMap>, IComparable<ILayoutItem>, IEnumerable<ILayoutItem>
    {
        protected int row = 0;
        protected int col = 0;
        protected LayoutItems items = null;
        [DefaultValue(1D)]
        private double scale = 1D;
        [DefaultValue(0D)]
        private double indent = 0D;

        [NonSerialized]
        public Func<ILayoutItem, double> CalcHeight;
        [NonSerialized]
        public Func<ILayoutItem, double> CalcWidth;
        [NonSerialized]
        protected Rectangle bound = new Rectangle();
        [NonSerialized]
        private double maxHeight;
        [NonSerialized]
        private double maxWidth;

        public event PropertyChangedEventHandler PropertyChanged;

        public LayoutMap()
        {
            items = new LayoutItems(this);
            items.ListChanged += OnItemsListChanged;
        }

        ~LayoutMap()
        {
            items.ListChanged -= OnItemsListChanged;
        }

        [XmlIgnore]
        public Rectangle Bound
        {
            get { return bound; }
            set { bound = value; }
        }

        public ILayoutMap TopMap
        {
            get { return LayoutMapTool.GetTopMap(this); }
        }

        public int Count
        {
            get { return items.Count; }
        }

        public ILayoutItem this[string property]
        {
            get { return LayoutMapTool.Get(this, property); }
        }

        public ILayoutItem this[int index]
        {
            get { return items[index]; }
        }

        public ILayoutItem this[int rowIndex, int colIndex]
        {
            get { return LayoutMapTool.Get(this, rowIndex, colIndex); }
        }

        public double Height
        {
            get { return LayoutMapTool.GetHeight(this, maxHeight, CalcHeight); }
            set { maxHeight = value; }
        }

        public double Width
        {
            get { return LayoutMapTool.GetWidth(this, maxWidth, CalcWidth); }
            set { maxWidth = value; }
        }

        public LayoutItems Items
        {
            get { return items; }
        }

        public int Row
        {
            get { return row; }
            set
            {
                if (row != value)
                {
                    row = value;
                    OnPropertyChanged(nameof(Row));
                }
            }
        }

        public int Col
        {
            get { return col; }
            set
            {
                if (col != value)
                {
                    col = value;
                    OnPropertyChanged(nameof(Col));
                }
            }
        }

        [XmlIgnore]
        public bool Visible
        {
            get { return LayoutMapTool.IsVisible(this); }
            set { }
        }

        [XmlIgnore]
        public bool FillWidth
        {
            get { return LayoutMapTool.IsFillWidth(this); }
            set { LayoutMapTool.SetFillWidth(this, value); }
        }

        [XmlIgnore]
        public bool FillHeight
        {
            get { return LayoutMapTool.IsFillHeight(this); }
            set { LayoutMapTool.SetFillHeight(this, value); }
        }

        public string Name
        {
            get { return string.Empty; }
            set { }
        }

        public ILayoutMap Map
        {
            get { return ((LayoutItems)Container)?.Map; }
        }

        public virtual double Scale
        {
            get { return Map?.Scale ?? scale; }
            set { scale = value; }
        }

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

        [XmlIgnore]
        public INotifyListChanged Container { get; set; }

        public bool Contains(string property)
        {
            return this[property] != null;
        }

        public void Sort()
        {
            items.Sort();
        }

        public bool Contains(ILayoutItem item)
        {
            return LayoutMapTool.Contains(this, item);
        }

        public virtual Rectangle GetBound()
        {
            return GetBound(maxWidth, maxHeight);
        }

        public virtual Rectangle GetBound(double maxWidth, double maxHeight)
        {
            LayoutMapTool.GetBound(this, maxWidth, maxHeight, CalcWidth, CalcHeight);
            return bound;
        }

        public virtual Rectangle GetBound(ILayoutItem item)
        {
            //maxWidth, maxHeight, 
            LayoutMapTool.GetBound(this, item, CalcWidth, CalcHeight);
            return item.Bound;
        }

        public virtual void Clear()
        {
            items.Clear();
        }

        public virtual void Replace(ILayoutItem oldColumn, ILayoutItem newColumn)
        {
            LayoutMapTool.Replace(oldColumn, newColumn);
        }

        public virtual void Grouping(ILayoutItem x, ILayoutItem y, LayoutAlignType type)
        {
            LayoutMapTool.Grouping(x, y, type);
        }

        public virtual void Move(ILayoutItem moved, ILayoutItem destination, LayoutAlignType anch, bool builGroup)
        {
            bound.Width = 0;
            LayoutMapTool.Move(moved, destination, anch, builGroup);
        }

        public virtual void Add(ILayoutItem column)
        {
            LayoutMapTool.Add(this, column);
        }

        public virtual void Insert(int index, ILayoutItem column)
        {
            column.Col = index;
            Insert(column, false);
        }

        public virtual void Insert(ILayoutItem column, bool inserRow = false)
        {
            LayoutMapTool.Insert(this, column, inserRow);
        }

        public virtual void InsertAfter(ILayoutItem column, ILayoutItem excolumn)
        {
            column.Row = excolumn.Row;
            column.Col = excolumn.Col + 1;
            Insert(column, false);
        }

        public virtual bool Remove(ILayoutItem column)
        {
            return LayoutMapTool.Remove(column);
        }

        public virtual void Reset()
        {
            LayoutMapTool.Reset(this);
        }

        protected virtual void OnItemsListChanged(object sender, ListChangedEventArgs e)
        {
            if (Map is LayoutMap)
            {
                ((LayoutMap)Map).OnItemsListChanged(sender, e);
            }
        }

        protected virtual void OnPropertyChanged(string property)
        {
            var args = new PropertyChangedEventArgs(property);
            PropertyChanged?.Invoke(this, args);
            Container?.OnPropertyChanged(this, args);
        }

        public IEnumerable<ILayoutItem> GetItems()
        {
            return LayoutMapTool.GetItems(this);
        }

        public int CompareTo(LayoutMap other)
        {
            return LayoutMapTool.Compare(this, other);
        }

        public int CompareTo(ILayoutItem other)
        {
            return LayoutMapTool.Compare(this, other);
        }

        public int CompareTo(object obj)
        {
            return LayoutMapTool.Compare(this, obj as ILayoutItem);
        }

        public IEnumerator<ILayoutItem> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }
    }
}

