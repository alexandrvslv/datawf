using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Avalonia;
using Avalonia.Controls;

namespace DataWF.Gui
{
    public class ToolContentItem : ToolItem
    {
        protected Control content;
        protected Rect contentBound;

        public ToolContentItem(Control content)
        {
            Content = content;
        }

        [DefaultValue(120)]
        public double ContentMinWidth { get; set; } = 120D;

        [XmlIgnore]
        public Control Content
        {
            get => content;
            set
            {
                if (content != value)
                {
                    if (Bar != null && content != null)
                        Bar.Children.Remove(content);
                    content = value;
                    if (Bar != null && content != null)
                        Bar.Children.Add(content);
                    CheckSize();
                }
            }
        }

        public Rect ContentBound => contentBound;

        public override bool Visible
        {
            get => base.IsVisible;
            set
            {
                if (content != null)
                    content.Visible = value;
                base.IsVisible = value;
            }
        }

        [XmlIgnore]
        public override Toolsbar Bar
        {
            get => base.Bar;
            set
            {
                if (Bar != value)
                {
                    if (Bar != null)
                    {
                        if (Content != null)
                            Bar.Children.Remove(Content);
                    }
                    base.Bar = value;
                    if (Bar != null)
                    {
                        if (Content != null)
                            Bar.Children.Add(Content);
                    }
                }
            }
        }

        public override void ApplyBound(Rect value)
        {
            base.ApplyBound(value);
            contentBound = new Rect(DisplayStyle.HasFlag(ToolItemDisplayStyle.Text)
               ? textBound.Right - 2
               : DisplayStyle.HasFlag(ToolItemDisplayStyle.Image)
                   ? imageBound.Right - 2
                   : Bound.X + 1,
                Bound.Y + (Bound.Height - contentBound.Height) / 2D,
                Bound.Width - (contentBound.Left - Bound.Left),
                contentBound.Height);
        }

        protected internal override void CheckSize(bool queue = true)
        {
            base.CheckSize(queue);
            if (Bar == null)
                return;
            if (content != null)
            {
                contentBound = new Rect(
                    new Point(DisplayStyle.HasFlag(ToolItemDisplayStyle.Text) ? textBound.Right : imageBound.Right, 0),
                    content.PreviousMeasure.Value);
                // contentBound.Width += indent;
            }
            else
            {
                contentBound = Rect.Empty;
            }


            //contentBound.Height = MinHeight - Bar.Indent;

            width = Math.Max(contentBound.Right, ContentMinWidth) + (Bar?.Indent ?? 0);
            height = Math.Max(Math.Max(textBound.Height, contentBound.Height), Bar.MinItemHeight) + Bar.Indent;
        }

        public override void Dispose()
        {
            //content?.Dispose();
            base.Dispose();
        }
    }
}
