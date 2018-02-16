using DataWF.Common;
using System;
using System.ComponentModel;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class DockPageBox : Canvas
    {
        private CellStyle style;
        private CellStyle sclose;
        private int padding = 3;
        private Orientation itemOrientation = Orientation.Horizontal;
        private Orientation orientation = Orientation.Horizontal;
        private DockPageList items;
        internal DockPage hover = null;
        internal DockPage closeHover = null;
        //private int min = 100;
        private Point p0;
        private bool mdown = false;
        private bool viewClose = true;
        private bool viewImage = true;
        private Rectangle brect = new Rectangle();

        public DockPageBox()
            : base()
        {
            items = new DockPageList(this);
            items.ListChanged += ListChanged;

            style = GuiEnvironment.StylesInfo["Page"];
            sclose = GuiEnvironment.StylesInfo["PageClose"];
        }

        public bool VisibleClose
        {
            get { return viewClose; }
            set
            {
                viewClose = value;
                AllocItems();
            }
        }

        public bool VisibleImage
        {
            get { return viewImage; }
            set
            {
                viewImage = value;
                AllocItems();
            }
        }

        public DockPageList Items
        {
            get { return items; }
        }

        public Orientation ItemOrientation
        {
            get { return itemOrientation; }
            set
            {
                if (itemOrientation == value)
                    return;
                itemOrientation = value;
                AllocItems();
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
                AllocItems();
            }
        }

        public int Pad
        {
            get { return padding; }
            set
            {
                padding = value;
                AllocItems();
            }
        }

        public CellStyle PageStyle
        {
            get { return style; }
            set
            {
                style = value;
                AllocItems();
            }
        }

        public CellStyle CloseStyle
        {
            get { return sclose; }
            set
            {
                sclose = value;
                AllocItems();
            }
        }

        public DockPanel Panel
        {
            get { return Parent as DockPanel; }
        }

        protected override void Dispose(bool disposing)
        {
            items.ListChanged -= ListChanged;
            items.Dispose();
            base.Dispose(disposing);
        }

        private void ListChanged(object sender, ListChangedEventArgs arg)
        {
            AllocItems();
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            AllocItems();
        }

        public void AllocItems()
        {
            var allocation = base.Bounds;
            double max = 350;
            int itemsCount = 0;
            foreach (var item in Items)
            {
                if (item.Visible)
                    itemsCount++;
            }
            double childLen = GetChildsWidth(max);
            if (orientation != ItemOrientation)
            {
                max = 100;
            }
            else if (orientation == Orientation.Horizontal && childLen > allocation.Width)
            {
                max = (allocation.Width - itemsCount * padding) / itemsCount;
                foreach (var item in Items)
                {
                    var w = GetToolWidth(item, max);
                    if (item.Visible && w < max)
                    {
                        max += (max - w) / itemsCount;
                    }
                }
            }
            else if (orientation == Orientation.Vertical && childLen > allocation.Height)
            {
                max = (allocation.Height - itemsCount * padding) / itemsCount;
            }

            double x = 0;//allocation.X;
            double y = 0;//allocation.Y;
            foreach (DockPage page in items)
            {
                if (page.Visible)
                {
                    //w.SizeRequest();
                    var ww = GetToolWidth(page, max);
                    var hh = 25D;
                    if (orientation != ItemOrientation)
                        ww = max;
                    if (ItemOrientation == Orientation.Vertical)
                    {
                        hh = ww;
                        ww = 25;
                    }
                    page.Widget.BackgroundColor = PageStyle.BackBrush.ColorSelect;
                    page.Bound = new Rectangle(x, y, ww < 1 ? 1 : ww, hh < 1 ? 1 : hh);
                    if (Orientation == Orientation.Horizontal)
                        x = page.Bound.Right + padding;
                    else
                        y = page.Bound.Bottom + padding;
                }
            }
            QueueDraw();
        }

        public event EventHandler<DockPageEventArgs> PageDrag;

        protected void OnPageDrag(DockPageEventArgs arg)
        {
            PageDrag?.Invoke(this, arg);
        }

        public event EventHandler<DockPageEventArgs> PageMove;

        protected void OnPageMove(DockPageEventArgs arg)
        {
            PageMove?.Invoke(this, arg);
            if ((mdown && p0.X != 0D && p0.Y != 0D) &&
                 (Math.Abs(p0.X - arg.Point.X) > 12 ||
                  Math.Abs(p0.Y - arg.Point.Y) > 12))
                OnPageDrag(arg);
        }

        public event EventHandler<DockPageEventArgs> PageClick;

        protected void OnPageClick(DockPageEventArgs arg)
        {
            if (!arg.Page.Active)
            {
                arg.Page.Active = !arg.Page.Active;
            }
            QueueDraw();
            PageClick?.Invoke(this, arg);
        }

        public event EventHandler<DockPageEventArgs> PageClose;

        public void ClosePage(DockPage page)
        {
            OnPageClose(new DockPageEventArgs(page));
        }

        protected void OnPageClose(DockPageEventArgs arg)
        {
            PageClose?.Invoke(this, arg);
            if (!arg.Page.HideOnClose)
            {
                arg.Page.Widget.Dispose();
            }
            Items.Remove(arg.Page);
        }

        public event EventHandler<DockPageEventArgs> PageHover;

        protected void OnPageHover(DockPageEventArgs arg)
        {
            PageHover?.Invoke(this, arg);
            hover = arg.Page;
            QueueDraw();
        }

        public event EventHandler<DockPageEventArgs> PageLeave;

        protected void OnPageLeave(DockPageEventArgs arg)
        {
            PageLeave?.Invoke(this, arg);
            hover = null;
            closeHover = null;
            p0.X = 0;
            QueueDraw();
        }

        public DockPageEventArgs HitTest(double x, double y)
        {
            foreach (DockPage page in items)
            {
                if (page.Visible && page.Bound.Contains(x, y))
                {
                    return new DockPageEventArgs(page) { Point = new Point(x, y) };
                }
            }
            return null;
        }

        protected override void OnMouseMoved(MouseMovedEventArgs args)
        {
            var arg = HitTest(args.X, args.Y);
            if (arg != null)
            {
                if (hover != arg.Page)
                {
                    if (hover != null)
                        OnPageLeave(arg);
                    OnPageHover(arg);
                }
                else
                {
                    OnPageMove(arg);
                }
                if (arg.Page.BoundClose.Contains(args.X, args.Y))
                {
                    closeHover = arg.Page;
                    QueueDraw();
                }
                else if (closeHover != null)
                {
                    closeHover = null;
                    QueueDraw();
                }
            }
            else if (hover != null)
            {
                OnPageLeave(new DockPageEventArgs(hover));
            }
            base.OnMouseMoved(args);
        }

        protected override void OnButtonReleased(ButtonEventArgs e)
        {
            var arg = HitTest(e.X, e.Y);
            if (arg != null)
            {
                if (e.Button == PointerButton.Left)
                {
                    if (VisibleClose && arg.Page.BoundClose.Contains(e.Position))
                    {
                        OnPageClose(arg);
                    }
                    else
                    {
                        OnPageClick(arg);
                    }
                }
            }
            mdown = false;
            base.OnButtonReleased(e);
        }

        protected override void OnButtonPressed(ButtonEventArgs args)
        {
            var arg = HitTest(args.X, args.Y);
            if (arg != null)
            {
                mdown = true;
                p0 = arg.Point;
            }
            base.OnButtonPressed(args);
        }

        protected override void OnMouseExited(EventArgs args)
        {
            base.OnMouseExited(args);
            if (closeHover != null)
            {
                closeHover = null;
            }
            if (hover != null)
                OnPageLeave(new DockPageEventArgs(hover));
            QueueDraw();
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            var context = GraphContext.Default;
            context.Context = ctx;

            if (ItemOrientation == Orientation.Horizontal)
                PageStyle.Angle = 0;
            else
                PageStyle.Angle = 90;

            foreach (DockPage page in items)
            {
                if (page.Visible)
                {
                    page.Draw(context);
                }
            }
            if (items.Count > 0)
            {
                if (orientation == Orientation.Horizontal)
                    brect = new Rectangle(0, Bounds.Height - 4, Bounds.Width, 5);
                else
                    brect = new Rectangle(Bounds.Width - 4, 0, 4, Bounds.Height);
                if (Panel.PagesAlign == LayoutAlignType.Bottom)
                    brect.Y = 0;
                context.FillRectangle(style, brect, CellDisplayState.Selected);//st.BackBrush.GetBrush(rectb, 
            }
        }

        public double GetChildsWidth(double max)
        {
            var w = 0D;
            foreach (DockPage dt in items)
            {
                if (dt.Visible)
                    w += GetToolWidth(dt, max) + padding;
            }
            return w;
        }

        public double GetChildsHeight()
        {
            double w = 0;
            foreach (DockPage dt in items)
            {
                w += dt.Bound.Height;
            }
            return w;
        }

        public double GetToolWidth(DockPage tool, double max)
        {
            var w = 18D;
            if (viewImage && (tool.Image != null || tool.Glyph != GlyphType.None))
                w += 20D;
            if (viewClose)
                w += 5D;
            //if(tool.VisibleText)
            w += GraphContext.MeasureString(tool.Label, style.Font, max).Width;
            return (w > max) ? max : w;
        }
    }
}
