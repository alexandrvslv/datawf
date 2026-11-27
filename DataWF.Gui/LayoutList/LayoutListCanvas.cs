using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace DataWF.Gui
{
    public class LayoutListCanvas : Canvas
    {
        //private ScrollAdjustment scrollVertical;

        public LayoutListCanvas(LayoutList layoutList)
        {
            List = layoutList;
        }

        public LayoutList List { get; set; }

        //public ScrollAdjustment ScrollHorizontal
        //{
        //    get => scrollHorizontal;
        //    set
        //    {
        //        scrollHorizontal = value;
        //        scrollHorizontal.ValueChanged += List.ScrollValueChanged;
        //    }
        //}
        //public ScrollAdjustment ScrollVertical
        //{
        //    get => scrollVertical;
        //    set
        //    {
        //        scrollVertical = value;
        //        scrollVertical.ValueChanged += List.ScrollValueChanged;
        //    }
        //}

        //public Point Location
        //{
        //    get { return new Point(ScrollHorizontal.Value, ScrollVertical.Value); }
        //}

        //protected override void SetScrollAdjustments(ScrollAdjustment horizontal, ScrollAdjustment vertical)
        //{
        //    //base.SetScrollAdjustments(horizontal, vertical);
        //    ScrollHorizontal = horizontal;
        //    ScrollVertical = vertical;
        //}

        public override void Render(DrawingContext ctx)
        {
            base.Render(ctx);
            Rect dirtyRect = Bounds;
            using (var context = new GraphContext(ctx))
            {
                context.FillRectangle(GuiEnvironment.Theme["List"].BaseColor, dirtyRect);

                if (List.ListSource == null)
                    return;
                List.OnDrawList(context, dirtyRect);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            List.RefreshBounds(false);
            var size = base.MeasureOverride(availableSize);
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

        protected override void OnPointerPressed(PointerPressedEventArgs args)
        {
            base.OnPointerPressed(args);
            List.CanvasButtonPress(args);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs args)
        {
            base.OnPointerReleased(args);
            List.CanvasButtonReleased(args);
        }

        protected override void OnPointerMoved(PointerEventArgs args)
        {
            base.OnPointerMoved(args);
            List.CanvasMouseMoved(args);
        }

        protected override void OnPointerExited(PointerEventArgs args)
        {
            base.OnPointerExited(args);
            List.CanvasMouseExited(args);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs args)
        {
            base.OnPointerWheelChanged(args);
            List.CanvasMouseScrolled(args);
        }

        protected override void OnLostFocus(RoutedEventArgs args)
        {
            base.OnLostFocus(args);
            List.CanvasLostFocus(args);
        }

        //protected override void OnKeyPressed(KeyEventArgs args)
        //{
        //    base.OnKeyPressed(args);
        //    List.CanvasKeyPressed(args);
        //}
    }
}