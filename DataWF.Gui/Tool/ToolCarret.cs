using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF;
using Avalonia;
using Avalonia.Input;

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

        public Rect CarretBound
        {
            get { return new Rect(Bound.Right - (CarretSize + 2), Bound.Top + (Bound.Height - CarretSize) / 2D, CarretSize, CarretSize); }
        }

        [DefaultValue(16D)]
        public Double CarretSize { get; set; } = 16D;

        [XmlIgnore]
        public CellDisplayState CarretState
        {
            get => carretState;
            set
            {
                if (carretState != value)
                {
                    carretState = value;
                    Bar?.InvalidateVisual();
                }
            }
        }

        public string CarretStyleName { get; set; } = "Window";

        [XmlIgnore]
        public CellStyle CarretStyle
        {
            get => carretStyle ??= GuiEnvironment.Theme[CarretStyleName];
            set
            {
                carretStyle = value;
                CarretStyleName = value?.Name;
            }
        }

        public bool CarretVisible { get; set; } = false;

        public GlyphType CarretGlyph { get; set; } = GlyphType.CaretDown;

        public event EventHandler CarretClick;

        protected override internal void OnPointerReleased(PointerReleasedEventArgs args)
        {
            var position = args.GetPosition(Bar);
            if (CarretVisible && CarretBound.Contains(position))
            {
                OnCarretClick(args);
            }
            else
            {
                base.OnPointerReleased(args);
            }
        }

        protected virtual void OnCarretClick(PointerEventArgs args)
        {
            CarretClick?.Invoke(this, args);
        }

        protected internal override void OnMouseMove(PointerEventArgs args)
        {
            base.OnMouseMove(args);
            var position = args.GetPosition(Bar);
            if (CarretVisible && CarretBound.Contains(position))
            {
                CarretState = CellDisplayState.Hover;
            }
            else
            {
                CarretState = CellDisplayState.Default;
            }
        }

        protected override internal void OnPointerExited(PointerEventArgs args)
        {
            base.OnPointerExited(args);
            CarretState = CellDisplayState.Default;
        }

        protected internal override void CheckSize(bool queue = true)
        {
            base.CheckSize(queue);
            width += CarretSize;
        }

        public override void Render(GraphContext context)
        {
            base.Render(context);
            if (CarretVisible)
            {
                var carret = CarretBound;
                context.DrawCell(CarretStyle, CarretGlyph, carret, carret, CarretState);
            }
        }
    }
}
