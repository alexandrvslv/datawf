using System;
using DataWF;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Media.Imaging;

namespace DataWF.Gui
{
    public class GlyphControl : Control, IGlyph
    {
        private CellDisplayState state;
        private CellStyle style;

        public GlyphControl()
        {
        }

        public CellStyle Style
        {
            get => style ??= GuiEnvironment.Theme["Window"];
            set => style = value;
        }

        public GlyphType Glyph { get; set; }

        public Bitmap Image { get; set; }

        public CellDisplayState State
        {
            get => state;
            set
            {
                if (state == value || !IsEnabled)
                    return;

                state = value;
                InvalidateVisual();
            }
        }

        public event EventHandler<EventArgs> Click;

        public override void Render(DrawingContext ctx)
        {
            base.Render(ctx);
            using (var context = new GraphContext(ctx))
                context.DrawCell(Style, Image ?? (object)Glyph, Bounds, Bounds.Inflate(new Thickness(-1, 1)), State);
        }

        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            State = CellDisplayState.Hover;
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            State = CellDisplayState.Default;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            State = CellDisplayState.Pressed;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            Click?.Invoke(this, e);
            State = CellDisplayState.Default;
        }
    }
}
