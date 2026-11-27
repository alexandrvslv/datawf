using DataWF;
using System;
using Avalonia;
using Avalonia.Controls;

namespace DataWF.Gui
{

    public class ToolMenuItem : ToolDropDown
    {
        public ToolMenuItem() : base()
        {
            Initialize();
        }

        public ToolMenuItem(EventHandler click) : base(click)
        {
            Initialize();
        }

        private void Initialize()
        {
            MenuAlign = LayoutAlignType.Right;
            DisplayStyle = ToolItemDisplayStyle.ImageAndText;
            CarretGlyph = GlyphType.CaretRight;
        }

        public override void Render(GraphContext context)
        {
            base.Render(context);
        }

        protected internal override void OnMouseEntered(EventArgs args)
        {
            base.OnMouseEntered(args);
            QueueShowMenu();
        }
    }
}
