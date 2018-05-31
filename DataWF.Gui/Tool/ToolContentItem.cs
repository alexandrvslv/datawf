using System;
using System.Xml.Serialization;
using Xwt;

namespace DataWF.Gui
{
    public class ToolContentItem : ToolItem
    {
        protected Widget content;
        protected Rectangle contentBound;

        public ToolContentItem(Widget content)
        {
            Content = content;
        }

        [XmlIgnore]
        public Widget Content
        {
            get { return content; }
            set
            {
                if (content != value)
                {
                    if (Bar != null && content != null)
                        Bar.RemoveChild(content);
                    content = value;
                    if (Bar != null && content != null)
                        Bar.AddChild(content);
                    CheckSize();
                }
            }
        }

        public Rectangle ContentBound => contentBound;

        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                if (content != null)
                    content.Visible = value;
                base.Visible = value;
            }
        }

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
                            Bar.RemoveChild(Content);
                    }
                    base.Bar = value;
                    if (Bar != null)
                    {
                        if (Content != null)
                            Bar.AddChild(Content);
                    }
                }
            }
        }

        public override void ApplyBound(Rectangle value)
        {
            base.ApplyBound(value);
            contentBound.X = DisplayStyle.HasFlag(ToolItemDisplayStyle.Text)
               ? textBound.Right - 2
               : DisplayStyle.HasFlag(ToolItemDisplayStyle.Image)
                   ? imageBound.Right - 2
                   : Bound.X + 1;
            contentBound.Y = Bound.Y + (Bound.Height - contentBound.Height) / 2D;
            contentBound.Width = Bound.Width - (contentBound.Left - Bound.Left);
        }

        protected internal override void CheckSize(bool queue = true)
        {
            base.CheckSize(queue);
            if (Bar == null)
                return;
            if (content != null)
            {
                contentBound.Size = content.Surface.GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);
                // contentBound.Width += indent;
            }
            else
            {
                contentBound.Width = 0;
            }

            contentBound.X = (DisplayStyle.HasFlag(ToolItemDisplayStyle.Text) ? textBound.Right : imageBound.Right);
            contentBound.Y = 0;
            //contentBound.Height = MinHeight - Bar.Indent;

            width = Math.Max(contentBound.Right, MinWidth) + (Bar?.Indent ?? 0);
            height = Math.Max(Math.Max(textBound.Height, contentBound.Height), MinHeight) + Bar.Indent;
        }

        public override void Dispose()
        {
            if (content != null)
                content.Dispose();
            base.Dispose();
        }
    }
}
