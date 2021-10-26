using System;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class GlyphWidget : Canvas, IGlyph
    {
        private CellDisplayState state;
        private CellStyle style;

        public GlyphWidget()
        {
        }

        public CellStyle Style
        {
            get { return style ?? (style = GuiEnvironment.Theme["Window"]); }
            set { style = value; }
        }

        public GlyphType Glyph { get; set; }

        public Image Image { get; set; }

        public CellDisplayState State
        {
            get { return state; }
            set
            {
                if (state == value || !Sensitive)
                    return;

                state = value;
                QueueDraw();
            }
        }

        public event EventHandler<EventArgs> Click;

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);
            var bound = new Rectangle(dirtyRect.Right - Bounds.Height, dirtyRect.Top, Bounds.Height, Bounds.Height);
            using (var context = new GraphContext(ctx))
                context.DrawCell(Style, Image ?? (object)Glyph, bound, bound.Inflate(-1, 1), State);
        }

        protected override void OnMouseEntered(EventArgs args)
        {
            base.OnMouseEntered(args);
            State = CellDisplayState.Hover;
        }

        protected override void OnMouseExited(EventArgs args)
        {
            base.OnMouseExited(args);
            State = CellDisplayState.Default;
        }

        protected override void OnButtonPressed(ButtonEventArgs args)
        {
            base.OnButtonPressed(args);
            State = CellDisplayState.Pressed;
        }

        protected override void OnButtonReleased(ButtonEventArgs args)
        {
            base.OnButtonReleased(args);
            Click?.Invoke(this, args);
            State = CellDisplayState.Default;
        }
    }
}
