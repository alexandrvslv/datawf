using System;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class ToolDropDown : ToolItem
    {
        private Menu menu;

        public ToolDropDown()
        {
        }

        public ToolDropDown(Widget content) : base(content)
        {
        }

        public Menu DropDown
        {
            get { return menu ?? (DropDown = new Menu()); }
            set
            {
                if (menu != value)
                {
                    menu = value;
                }
            }
        }

        public MenuItemCollection DropDownItems
        {
            get { return DropDown.Items; }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            ShowMenu();
        }

        protected void ShowMenu()
        {
            if (menu != null)
            {
                var windowCoord = Bar.ConvertToScreenCoordinates(Bound.BottomLeft);
                menu.Popup();
            }
        }

    }
}
