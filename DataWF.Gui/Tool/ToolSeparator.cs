using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class ToolSeparator : ToolItem
    {
        public ToolSeparator()
        {
            DisplayStyle = ToolItemDisplayStyle.Image;
            CheckSize();
        }

        protected internal override void CheckSize(bool queue = true)
        {
            width = 6;
            Height = 6;
        }

        public override void OnDraw(GraphContext context)
        {
            //context.Context.Save();
            //context.Context.SetColor(style.FontBrush.Color);
            //context.Context.SetLineWidth(2);
            //context.Context.MoveTo(Bound.X + Bound.Width / 2, Bound.Y + 2);
            //context.Context.LineTo(Bound.X + Bound.Width / 2, Bound.Bottom - 2);
            //context.Context.Restore();
        }

        public override void Localize()
        {
        }

    }
}
