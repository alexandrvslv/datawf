using DataWF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace DataWF.Gui
{
    public class LayoutGroup : DefaultItem, IComparable, IDisposable
    {
        protected int startIndex = -1;
        protected int endIndex = -1;
        protected bool isExpand = true;
        protected bool visible = true;
        protected DateTime stamp;
        private string text;
        private FormattedText FormattedText;
        protected Dictionary<LayoutColumn, decimal> cache = new Dictionary<LayoutColumn, decimal>();
        public LayoutListInfo Info;
        public int GridRows;
        public Rect Bound;
        public Size TextSize;

        public LayoutGroup()
        { }

        public Dictionary<LayoutColumn, decimal> CollectedCache
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

        public string TextValue
        {
            get => text;
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
            get => endIndex;
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
            get => startIndex;
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
            get => isExpand;
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

        public bool IsVisible
        {
            get => startIndex < 0 || endIndex < 0 ? false : visible;
            set => visible = value;
        }

        public DateTime Stamp
        {
            get => stamp;
            set => stamp = value;
        }

        public bool Contains(int index)
        {
            return index <= endIndex && index >= startIndex;
        }

        public int CompareTo(object obj)
        {
            return ListHelper.Compare(TextValue, ((LayoutGroup)obj).TextValue, null);
        }

        public FormattedText GetFormattedText()
        {
            FormattedText ??= new FormattedText() { Typeface = Info.StyleGroup.Typeface };
            var toString = ToString();
            if (toString != FormattedText.Text)
            {
                FormattedText.Text = toString;
                FormattedText.Width = -1;
                TextSize = FormattedText.GetSize();
            }
            return FormattedText;
        }

        public void Dispose()
        {
            FormattedText?.Dispose();
            FormattedText = null;
        }
    }
}
