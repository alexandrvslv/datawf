using System;
using System.Collections.Generic;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class ToolDropDown : ToolItem
    {
        private Menubar menu;

        public ToolDropDown()
        {
        }

        public ToolDropDown(EventHandler click) : base(click)
        {
        }

        public ToolDropDown(params ToolItem[] items)
        {
            DropDownItems.AddRange(items);
        }

        public ToolDropDown(Widget content) : base(content)
        {
        }

        public event EventHandler DropDownOpened;

        public Menubar DropDown
        {
            get { return menu ?? (DropDown = new Menubar { Name = Name }); }
            set
            {
                if (menu != value)
                {
                    menu = value;
                    menu.OwnerItem = this;
                }
            }
        }

        public ToolLayoutMap DropDownItems
        {
            get { return DropDown.Items; }
        }

        public bool HasDropDown
        {
            get { return menu?.Items.Count > 0; }
        }

        public event EventHandler<ToolItemEventArgs> ItemClick
        {
            add { DropDownItems.Bar.ItemClick += value; }
            remove { DropDownItems.Bar.ItemClick -= value; }
        }

        public override void Localize()
        {
            base.Localize();
            if (menu != null)
            {
                if (string.IsNullOrEmpty(menu.Name))
                    menu.Name = Name;
                menu.Localize();
            }
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
                if (menu.Visible)
                {
                    menu.Hide();
                }
                else
                {
                    menu.Popup(Bar, Bound);
                    DropDownOpened?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
