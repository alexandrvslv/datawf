using System;
using System.Collections.Generic;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class ToolDropDown : ToolItem
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
            DropDownItems.AddRange(items);
        }

        public Rectangle CarretBound
        {
            get { return new Rectangle(new Point(Bound.Right - CarretSize.Width, Bound.Top + (Bound.Height - CarretSize.Height) / 2D), CarretSize); }
        }

        public CellDisplayState CarretState { get; set; } = CellDisplayState.Default;

        public CellStyle CarretStyle { get; set; } = GuiEnvironment.Theme["Window"];

        public Size CarretSize { get; set; } = new Size(16, 16);

        public bool CarretVisible { get; set; } = false;

        public GlyphType CarretGlyph { get; set; } = GlyphType.CaretDown;

        public event EventHandler CarretClick;

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
                    CarretVisible = true;
                }
            }
        }

        public ToolItem DropDownItems
        {
            get { return DropDown.Items; }
        }

        public bool HasDropDown
        {
            get { return menu?.Items.Count > 0; }
        }

        public LayoutAlignType MenuAlign { get => menuAlign; set => menuAlign = value; }

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
            if (menu != null)
            {
                Bar.CurrentMenubar = menu.Visible ? null : menu;
            }
        }

        protected override internal void OnMouseEntered(EventArgs args)
        {
            base.OnMouseEntered(args);
        }

        protected override internal void OnMouseExited(EventArgs args)
        {
            base.OnMouseExited(args);
            CarretState = CellDisplayState.Default;
        }

        protected override internal void OnButtonPressed(ButtonEventArgs args)
        {
            base.OnButtonPressed(args);
        }

        protected override internal void OnButtonReleased(ButtonEventArgs args)
        {
            base.OnButtonReleased(args);
            if (CarretBound.Contains(args.Position))
            {
                CarretClick?.Invoke(this, args);
            }
        }

        protected internal override void CheckSize(bool queue = true)
        {
            base.CheckSize(queue);
            width += CarretSize.Width;
        }

        public override void OnDraw(GraphContext context)
        {
            base.OnDraw(context);
            if (CarretVisible)
            {
                var carret = CarretBound;
                context.DrawCell(CarretStyle, CarretGlyph, carret, carret, CellDisplayState.Default);
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
