﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace DataWF.Gui
{
    public class ToolDropDown : ToolCarret
    {
        protected Menubar menu;
        private LayoutAlignType menuAlign = LayoutAlignType.Bottom;


        public ToolDropDown() : base()
        {
        }

        public ToolDropDown(EventHandler click) : base(click)
        {
        }

        public ToolDropDown(params ToolItem[] items) : this()
        {
            DropDown = new Menubar();
            DropDown.Items.AddRange(items);
        }

        public event EventHandler DropDownOpened;

        public Menubar DropDown
        {
            get { return menu; }//?? (DropDown = new Menubar { Name = Name })
            set
            {
                if (menu != value)
                {
                    menu = value;
                    menu.OwnerItem = this;
                    CarretVisible = true;
                }
            }
        }

        public ToolItem DropDownItems
        {
            get { return DropDown?.Items; }
        }

        public bool HasDropDown
        {
            get { return menu?.Items.Count > 0; }
        }

        public LayoutAlignType MenuAlign { get => menuAlign; set => menuAlign = value; }

        public virtual void OnItemClick(ToolItemEventArgs e)
        {
            ItemClick?.Invoke(this, e);
            Bar.OnItemClick(e.Item);
        }

        public event EventHandler<ToolItemEventArgs> ItemClick;

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
            QueueShowMenu();
        }

        protected void QueueShowMenu()
        {
            if (menu != null)
            {
                Bar.CurrentMenubar = menu.Visible ? null : menu;
            }
        }

        public override void Dispose()
        {
            menu?.Dispose();
            base.Dispose();
        }

        protected virtual void OnDropDownOpened()
        {
            DropDownOpened?.Invoke(this, EventArgs.Empty);
        }

        public virtual void ShowMenu()
        {
            if (menu != null)
            {
                if (!menu.Visible)
                {
                    var point = MenuAlign == LayoutAlignType.Left
                        ? Bound.TopLeft
                        : MenuAlign == LayoutAlignType.Right
                        ? Bound.TopRight
                        : Bound.BottomLeft;
                    menu.Popup(Bar, point);
                    OnDropDownOpened();
                }
            }
        }

        public void HideMenu()
        {
            if (menu != null)
            {
                if (menu.Visible)
                {
                    menu.Hide();
                }
            }
        }


    }
}
