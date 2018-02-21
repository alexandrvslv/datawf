using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class LayoutItem : ILayoutItem, IComparable
    {
        private static readonly double min = 5;
        protected double height = 22D;
        protected double width = 120D;
        protected int row;
        protected int col;
        protected bool visible = true;
        protected bool fillW;
        protected bool fillH;
        protected string name;
        protected object tag;
        protected Rectangle bound;
        private INotifyListChanged container;

        [XmlIgnore]
        public virtual Rectangle Bound
        {
            get { return bound; }
            set { bound = value; }
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
            get { return height; }
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
            get { return width; }
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
            get { return visible; }
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
            get { return fillW; }
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
            get { return fillH; }
            set
            {
                if (fillH == value)
                    return;
                fillH = value;
                OnPropertyChanged(nameof(FillHeight));
            }
        }

        [XmlIgnore]
        public ILayoutMap TopMap
        {
            get
            {
                ILayoutItem temp = this;
                while (temp.Map != null)
                {
                    temp = temp.Map;
                }
                return temp as ILayoutMap;
            }
        }

        [XmlIgnore]
        public ILayoutMap Map
        {
            get { return ((LayoutItems)container)?.Map; }
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

        public override string ToString()
        {
            return string.Format("({0},{1}) {2}", row, col, name);
        }

        #region IComparable implementation

        public int CompareTo(object obj)
        {
            return LayoutMapHelper.Compare(this, obj as ILayoutItem);
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
    }
}

