using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class ToolDropDown : ToolItem
    {
        protected Menubar menu;
        private LayoutAlignType menuAlign = LayoutAlignType.Bottom;
        private CellStyle carretStyle;
        private CellDisplayState carretState = CellDisplayState.Default;

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
            get { return new Rectangle(Bound.Right - (CarretSize + 2), Bound.Top + (Bound.Height - CarretSize) / 2D, CarretSize, CarretSize); }
        }

        [DefaultValue(16D)]
        public Double CarretSize { get; set; } = 16D;

        [XmlIgnore]
        public CellDisplayState CarretState
        {
            get { return carretState; }
            set
            {
                if (carretState != value)
                {
                    carretState = value;
                    Bar?.QueueDraw();
                }
            }
        }

        public string CarretStyleName { get; set; } = "Window";

        [XmlIgnore]
        public CellStyle CarretStyle
        {
            get { return carretStyle ?? (carretStyle = GuiEnvironment.Theme[CarretStyleName]); }
            set
            {
                carretStyle = value;
                CarretStyleName = value?.Name;
            }
        }

        public bool CarretVisible { get; set; } = false;

        public GlyphType CarretGlyph { get; set; } = GlyphType.CaretDown;

        public event EventHandler CarretClick;

        public event EventHandler DropDownOpened;

        [XmlIgnore]
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
            set { DropDown.Items.AddRange(value); }
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
            QueueShowMenu();
        }

        protected void QueueShowMenu()
        {
            if (menu != null)
            {
                Bar.CurrentMenubar = menu.Visible ? null : menu;
            }
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
            if (CarretVisible && CarretBound.Contains(args.Position))
            {
                OnCarretClick(args);
            }
            else
            {
                base.OnButtonReleased(args);
            }
        }

        protected internal override void OnMouseMove(MouseMovedEventArgs args)
        {
            base.OnMouseMove(args);
            if (CarretVisible && CarretBound.Contains(args.Position))
            {
                CarretState = CellDisplayState.Hover;
            }
            else
            {
                CarretState = CellDisplayState.Default;
            }
        }

        protected virtual void OnCarretClick(ButtonEventArgs args)
        {
            CarretClick?.Invoke(this, args);
        }

        protected internal override void CheckSize(bool queue = true)
        {
            base.CheckSize(queue);
            width += CarretSize;
        }

        public override void OnDraw(GraphContext context)
        {
            base.OnDraw(context);
            if (CarretVisible)
            {
                var carret = CarretBound;
                context.DrawCell(CarretStyle, CarretGlyph, carret, carret, CarretState);
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
