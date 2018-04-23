using DataWF.Common;
using System;
using Xwt;
using Xwt.Drawing;

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
            indent = 0;
        }

        public override void OnDraw(GraphContext context)
        {
            base.OnDraw(context);
        }
    }
}
