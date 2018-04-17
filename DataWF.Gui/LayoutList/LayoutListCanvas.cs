using System;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class LayoutListCanvas : Canvas
    {
        private ScrollAdjustment scrollHorizontal;
        private ScrollAdjustment scrollVertical;

        public LayoutListCanvas(LayoutList layoutList)
        {
            List = layoutList;
        }

        public LayoutList List { get; set; }

        public ScrollAdjustment ScrollHorizontal
        {
            get { return scrollHorizontal; }
            set
            {
                scrollHorizontal = value;
                scrollHorizontal.ValueChanged += List.ScrollValueChanged;
            }
        }
        public ScrollAdjustment ScrollVertical
        {
            get { return scrollVertical; }
            set
            {
                scrollVertical = value;
                scrollVertical.ValueChanged += List.ScrollValueChanged;
            }
        }

        protected override bool SupportsCustomScrolling { get { return true; } }

        public Point Location
        {
            get { return new Point(ScrollHorizontal.Value, ScrollVertical.Value); }
        }

        protected override void SetScrollAdjustments(ScrollAdjustment horizontal, ScrollAdjustment vertical)
        {
            //base.SetScrollAdjustments(horizontal, vertical);
            ScrollHorizontal = horizontal;
            ScrollVertical = vertical;
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);
            if (List.ListSource == null)
                return;
            GraphContext.Default.Context = ctx;
            List.OnDrawList(GraphContext.Default, dirtyRect);
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            List.RefreshBounds(false);
        }

        protected override void OnButtonPressed(ButtonEventArgs args)
        {
            base.OnButtonPressed(args);
            List.CanvasButtonPress(args);
        }

        protected override void OnButtonReleased(ButtonEventArgs args)
        {
            base.OnButtonReleased(args);
            List.CanvasButtonReleased(args);
        }

        protected override void OnMouseMoved(MouseMovedEventArgs args)
        {
            base.OnMouseMoved(args);
            List.CanvasMouseMoved(args);
        }

        protected override void OnMouseExited(EventArgs args)
        {
            base.OnMouseExited(args);
            List.CanvasMouseExited(args);
        }

        protected override void OnMouseScrolled(MouseScrolledEventArgs args)
        {
            base.OnMouseScrolled(args);
            List.CanvasMouseScrolled(args);
        }

        protected override void OnLostFocus(EventArgs args)
        {
            base.OnLostFocus(args);
            List.CanvasLostFocus(args);
        }

        //protected override void OnKeyPressed(KeyEventArgs args)
        //{
        //    base.OnKeyPressed(args);
        //    List.CanvasKeyPressed(args);
        //}

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            var size = base.OnGetPreferredSize(widthConstraint, heightConstraint);
            //if (List.AutoSize && List.ListInfo != null)
            //{
            //    if (List.ListInfo.Columns.Bound.Width == 0)
            //        List.ListInfo.GetColumnsBound(widthConstraint.AvailableSize, null, null);
            //    var content = List.GetContentBound();
            //    size = new Size(content.Width > MinWidth ? content.Width : MinWidth,
            //                    content.Height > MinHeight ? content.Height : MinHeight);
            //}
            //else
            {
                size = new Size(200, 100);
            }
            return size;
        }
    }
}