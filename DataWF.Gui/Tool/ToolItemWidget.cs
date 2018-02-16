using System;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class ToolItemWidget : Canvas
    {
        ToolItem tool;
        public ToolItemWidget(ToolItem tool)
        {
            Tool = tool;
        }

        public ToolItem Tool
        {
            get { return tool; }
            set
            {
                tool = value;
                tool.Bar = this;
            }
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            base.OnGetPreferredSize(widthConstraint, heightConstraint);
            tool.CheckSize();
            return new Size(tool.Width, tool.Height);
        }

        protected override void OnReallocate()
        {
            tool.Bound = new Rectangle(0, 0, Size.Width, Size.Height);
            if (tool.Content != null)
                SetChildBounds(tool.Content, tool.GetContentBound());
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            GraphContext.Default.Context = ctx;
            tool.OnDraw(GraphContext.Default);
        }

        protected override void OnButtonPressed(ButtonEventArgs args)
        {
            base.OnButtonPressed(args);
            tool.OnButtonPressed(args);
        }

        protected override void OnButtonReleased(ButtonEventArgs args)
        {
            base.OnButtonReleased(args);
            tool.OnButtonReleased(args);
        }

        protected override void OnMouseEntered(EventArgs args)
        {
            base.OnMouseEntered(args);
            tool.OnMouseEntered(args);
        }

        protected override void OnMouseExited(EventArgs args)
        {
            base.OnMouseExited(args);
            tool.OnMouseExited(args);
        }
    }
}
