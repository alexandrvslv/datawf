using System;
using System.Linq;
using Xwt;
using Xwt.Drawing;
using System.Collections.Generic;

namespace Mono.TextEditor.Theatrics
{
    /// <summary>
    /// A scrolled window with the ability to put widgets beside the scrollbars.
    /// </summary>
    public class SmartScrolledWindow : Canvas
    {
        public enum ChildPosition
        {
            Top,
            Bottom,
            Left,
            Right
        }
        VScrollbar vScrollBar;
        HScrollbar hScrollBar;
        Dictionary<Widget, ChildPosition> positions = new Dictionary<Widget, ChildPosition>();


        public ScrollAdjustment Vadjustment
        {
            get { return vScrollBar.ScrollAdjustment; }
        }

        public ScrollAdjustment Hadjustment
        {
            get { return hScrollBar.ScrollAdjustment; }
        }

        public bool BorderVisible { get; set; }


        public SmartScrolledWindow(VScrollbar vScrollBar = null)
        {
            vScrollBar = vScrollBar ?? new VScrollbar() { };
            AddChild(vScrollBar);
            Vadjustment.ValueChanged += HandleAdjustmentChanged;

            hScrollBar = new HScrollbar();
            AddChild(hScrollBar);
            Hadjustment.ValueChanged += HandleAdjustmentChanged;
        }

        public void ReplaceVScrollBar(VScrollbar widget)
        {
            if (vScrollBar != null)
            {
                RemoveChild(vScrollBar);
                vScrollBar.Dispose();
            }
            vScrollBar = widget;
            vScrollBar.Visible = true;
            AddChild(vScrollBar);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        void HandleAdjustmentChanged(object sender, EventArgs e)
        {
            var adjustment = (ScrollAdjustment)sender;
            var scrollbar = adjustment == Vadjustment ? (Widget)vScrollBar : hScrollBar;
            if (!(scrollbar is Scrollbar))
                return;
            bool newVisible = adjustment.UpperValue - adjustment.LowerValue > adjustment.PageSize;
            if (scrollbar.Visible != newVisible)
            {
                scrollbar.Visible = newVisible;
                QueueForReallocate();
            }
        }

        public void AddChild(Widget child, ChildPosition position)
        {
            AddChild(child);
            positions[child] = position;
        }


        protected override void OnReallocate()
        {
            base.OnReallocate();

            int margin = BorderVisible ? 1 : 0;
            var vwidth = vScrollBar.Visible ? vScrollBar.WidthRequest : 0D;
            var hheight = hScrollBar.Visible ? hScrollBar.HeightRequest : 0D;
            var childRectangle = new Xwt.Rectangle(Bounds.X + margin, Bounds.Y + margin, Bounds.Width - vwidth - margin * 2, Bounds.Height - hheight - margin * 2);

            if (vScrollBar.Visible)
            {
                double vChildTopHeight = -1;
                foreach (var child in positions)
                {
                    if (child.Value == ChildPosition.Top)
                    {
                        SetChildBounds(child.Key, new Rectangle(childRectangle.RightInside(), childRectangle.Y + vChildTopHeight, Bounds.Width - vwidth, child.Key.HeightRequest));
                        vChildTopHeight += child.Key.HeightRequest;
                    }
                }
                var v = vScrollBar is Scrollbar && hScrollBar.Visible ? hScrollBar.HeightRequest : 0;
                SetChildBounds(vScrollBar, new Rectangle(childRectangle.X + childRectangle.Width + margin, childRectangle.Y + vChildTopHeight, vwidth, Bounds.Height - v - vChildTopHeight - margin));
                Vadjustment.Value = System.Math.Max(System.Math.Min(Vadjustment.UpperValue - Vadjustment.PageSize, Vadjustment.Value), Vadjustment.LowerValue);
            }

            if (hScrollBar.Visible)
            {
                var v = vScrollBar.Visible ? vScrollBar.WidthRequest : 0;
                SetChildBounds(hScrollBar, new Rectangle(Bounds.X, childRectangle.Y + childRectangle.Height + margin, Bounds.Width - v, hheight));
                hScrollBar.Value = System.Math.Max(System.Math.Min(Hadjustment.UpperValue - Hadjustment.PageSize, hScrollBar.Value), Hadjustment.LowerValue);
            }
        }

        static double Clamp(double min, double val, double max)
        {
            return System.Math.Max(min, System.Math.Min(val, max));
        }

        protected override void OnMouseScrolled(MouseScrolledEventArgs args)
        {
            var alloc = Bounds;
            if ((args.Direction == ScrollDirection.Left
                 || args.Direction == ScrollDirection.Right)
                && hScrollBar.Visible)
                Hadjustment.AddValueClamped(args.Direction == ScrollDirection.Right ? 10 : -10);

            if ((args.Direction == ScrollDirection.Up
                 || args.Direction == ScrollDirection.Down) && vScrollBar.Visible)
                Vadjustment.AddValueClamped(args.Direction == ScrollDirection.Down ? 10 : -10);

            base.OnMouseScrolled(args);
        }

        protected override Size OnGetPreferredSize(SizeConstraint width, SizeConstraint height)
        {
            //if (Child != null)
            //    Child.SizeRequest();
            //vScrollBar.SizeRequest();
            //hScrollBar.SizeRequest();
            return base.OnGetPreferredSize(width, height);
        }

        protected override void OnDraw(Xwt.Drawing.Context cr, Rectangle rect)
        {
            if (BorderVisible)
            {
                {
                    cr.SetLineWidth(1);

                    var alloc = rect;
                    var right = alloc.RightInside();
                    var bottom = alloc.BottomInside();

                    cr.SharpLineX(alloc.X, alloc.Y, alloc.X, bottom);
                    cr.SharpLineX(right, alloc.Y, right, bottom);

                    cr.SharpLineY(alloc.X, alloc.Y, right, alloc.Y);
                    cr.SharpLineY(alloc.X, bottom, right, bottom);

                    cr.SetColor(Colors.Gray);
                    cr.Stroke();
                }
            }
        }

    }
}

