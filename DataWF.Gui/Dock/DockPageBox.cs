using DataWF.Common;
using System;
using System.ComponentModel;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class DockPageBox : Toolsbar
    {
        private Orientation orientation = Orientation.Horizontal;
        //private int min = 100;
        private bool viewClose = true;
        private bool viewImage = true;

        public DockPageBox() : base()
        {
            Items.StyleName = "Window";
        }

        public bool VisibleClose
        {
            get { return viewClose; }
            set
            {
                if (viewClose == value)
                    return;
                viewClose = value;
                foreach (DockPage item in Items.GetItems())
                {
                    item.CarretVisible = viewClose;
                }
            }
        }

        public Orientation Orientation
        {
            get { return orientation; }
            set
            {
                if (orientation == value)
                    return;
                orientation = value;
                QueueForReallocate();
            }
        }

        public DockPanel Panel
        {
            get { return Parent as DockPanel; }
        }

        public DockItem DockItem
        {
            get { return Panel?.DockItem; }
        }

        public event EventHandler<DockPageEventArgs> PageDrag;

        protected void OnPageDrag(DockPageEventArgs arg)
        {
            PageDrag?.Invoke(this, arg);
        }

        protected internal override void OnItemMove(ToolItemEventArgs args)
        {
            base.OnItemMove(args);

            //if ((mdown && p0.X != 0D && p0.Y != 0D) &&
            //     (Math.Abs(p0.X - arg.Point.X) > 12 ||
            //      Math.Abs(p0.Y - arg.Point.Y) > 12))
            //    OnPageDrag(arg);
        }

        public event EventHandler<DockPageEventArgs> PageClose;

        public void ClosePage(DockPage page)
        {
            OnPageClose(new DockPageEventArgs(page));
        }

        protected void OnPageClose(DockPageEventArgs arg)
        {
            if (arg.Page.Widget is IDockContent)
            {
                if (!((IDockContent)arg.Page.Widget).Closing())
                    return;
            }
            PageClose?.Invoke(this, arg);
            if (!arg.Page.HideOnClose)
            {
                arg.Page.Widget.Dispose();
            }
            Items.Remove(arg.Page);
            Panel.RemovePage(arg.Page);
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);
            using (var context = new GraphContext(ctx))
            {
                if (items.Count > 0)
                {
                    Rectangle brect = orientation == Orientation.Horizontal
                        ? brect = new Rectangle(0, Bounds.Height - 4, Bounds.Width, 5)
                        : brect = new Rectangle(Bounds.Width - 4, 0, 4, Bounds.Height);
                    if (Panel.PagesAlign == LayoutAlignType.Bottom)
                        brect.Y = 0;
                    context.FillRectangle(items.Style, brect, CellDisplayState.Selected);//st.BackBrush.GetBrush(rectb, 
                }
            }
        }
    }
}
