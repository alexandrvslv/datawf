using System;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class ToolSplit : ToolDropDown
    {
        public ToolSplit() : base(new ToolItemWidget(new ToolItem() { MinWidth = 16, MinHeight = 16, Glyph = GlyphType.CaretDown }))
        {
            ToolItem.Click += OnClickSplit;
        }

        public ToolSplit(EventHandler click) : this()
        {
            ButtonClick += click;
        }

        public ToolItem ToolItem
        {
            get { return ((ToolItemWidget)content).Tool; }
        }

        public event EventHandler ButtonClick;

        protected override void OnClick(EventArgs e)
        {
            if (ToolItem.State == CellDisplayState.Default)
                ButtonClick?.Invoke(this, e);
        }

        protected void OnClickSplit(object sender, EventArgs e)
        {
            if (menu != null)
            {
                Bar.CurrentMenubar = menu.Visible ? null : menu;
            }
        }
    }
}
