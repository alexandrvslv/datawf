using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class LayoutGroup : IComparable, IContainerNotifyPropertyChanged, IDisposable
    {
        protected int startIndex = -1;
        protected int endIndex = -1;
        protected bool isExpand = true;
        protected bool visible = true;
        protected DateTime stamp;
        private object text;
        private TextLayout textLayout;
        protected Dictionary<LayoutColumn, object> cache = new Dictionary<LayoutColumn, object>();
        public LayoutListInfo Info;
        public int GridRows;
        public Rectangle Bound;
        public Size TextSize;

        public LayoutGroup()
        { }

        public Dictionary<LayoutColumn, object> CollectedCache
        {
            get { return cache; }
        }

        public override string ToString()
        {
            return string.Format("{0}{1}{2}",
                Info.GroupName ? Header + " " : string.Empty,
                TextValue,
                Info.GroupCount ? " (" + Count + ")" : string.Empty);
        }

        protected string Property { get; set; } = string.Empty;

        public string Header { get; set; } = string.Empty;

        public object Value { get; set; } = string.Empty;

        public object TextValue
        {
            get { return text; }
            set
            {
                if (text != value)
                {
                    text = value;
                    OnPropertyChanged(nameof(TextValue));
                }
            }
        }

        public int IndexEnd
        {
            get { return endIndex; }
            set
            {
                if (endIndex != value)
                {
                    endIndex = value;
                    OnPropertyChanged(nameof(IndexEnd));
                }
            }
        }

        public int IndexStart
        {
            get { return startIndex; }
            set
            {
                if (startIndex != value)
                {
                    startIndex = value;
                    OnPropertyChanged(nameof(IndexStart));
                }
            }
        }

        public bool IsExpand
        {
            get { return isExpand; }
            set
            {
                if (isExpand != value)
                {
                    isExpand = value;
                    OnPropertyChanged(nameof(IsExpand));
                }
            }
        }

        public int Count
        {
            get { return endIndex - startIndex + 1; }
        }

        public bool Visible
        {
            get { return startIndex < 0 || endIndex < 0 ? false : visible; }
            set { visible = value; }
        }

        public DateTime Stamp
        {
            get { return stamp; }
            set { stamp = value; }
        }

        [XmlIgnore]
        public INotifyListChanged Container { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string property)
        {
            Container?.OnPropertyChanged(this, new PropertyChangedEventArgs(property));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public bool Contains(int index)
        {
            return index <= endIndex && index >= startIndex;
        }

        public int CompareTo(object obj)
        {
            return ListHelper.Compare(TextValue, ((LayoutGroup)obj).TextValue, null, false);
        }

        public TextLayout GetTextLayout()
        {
            if (textLayout == null)
            {
                textLayout = new TextLayout() { Font = Info.StyleGroup.Font };
            }
            var toString = ToString();
            if (toString != textLayout.Text)
            {
                textLayout.Text = toString;
                TextSize = textLayout.GetSize();
            }
            return textLayout;
        }

        public void Dispose()
        {
            textLayout?.Dispose();
            textLayout = null;
        }
    }
}
