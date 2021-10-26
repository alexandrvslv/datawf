using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class ToolCarret : ToolItem
    {
        private CellStyle carretStyle;
        private CellDisplayState carretState = CellDisplayState.Default;

        public ToolCarret() : base()
        {
        }

        public ToolCarret(EventHandler click) : base(click)
        {
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

        protected virtual void OnCarretClick(ButtonEventArgs args)
        {
            CarretClick?.Invoke(this, args);
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

        protected override internal void OnMouseExited(EventArgs args)
        {
            base.OnMouseExited(args);
            CarretState = CellDisplayState.Default;
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
    }
}
