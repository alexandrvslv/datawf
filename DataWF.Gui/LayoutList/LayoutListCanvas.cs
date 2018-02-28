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
            MinWidth = 100;
            MinHeight = 100;
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
            List.ButtonPress(args);
        }

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

