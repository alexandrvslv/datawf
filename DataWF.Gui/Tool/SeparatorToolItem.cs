using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class SeparatorToolItem : ToolItem
    {
        public SeparatorToolItem()
        {
            MinWidth = 6D;
            indent = 2D;
            DisplayStyle = ToolItemDisplayStyle.Image;
            CheckSize();
        }

        internal protected override void OnDraw(GraphContext context)
        {
            context.Context.Save();
            context.Context.SetColor(style.FontBrush.Color);
            context.Context.SetLineWidth(2);
            context.Context.MoveTo(Bound.X + Bound.Width / 2, Bound.Y + 2);
            context.Context.LineTo(Bound.X + Bound.Width / 2, Bound.Bottom - 2);
            context.Context.Restore();
        }

    }
}
