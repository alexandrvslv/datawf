using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xwt;
using DataWF.Common;
using Xwt.Drawing;

namespace DataWF.Gui
{
    [Flags]
    public enum DockBoxState
    {
        Default = 0,
        InProcess = 1,
        SizeLR = 2,
        SizeUD = 4,
        Move = 8
    }

    public class DockBox : Canvas, IDockContainer
    {
        private DockMap map;
        private EventHandler childFocusHandler;
        private DockPage page = null;
        private DockMapItem pContent;
        private DockBoxState state = DockBoxState.Default;
        private Point cach;
        private Rectangle stateMove;
        private Rectangle stateSize;
        private DockBoxHitTest hitLeft = null;
        private DockBoxHitTest hitRight = null;
        private DockBoxHitTest hitTop = null;
        private DockBoxHitTest hitBottom = null;

        public event EventHandler<DockPageEventArgs> PageSelected;

        public DockBox()
            : base()
        {
            Map = new DockMap();
            pContent = GetDockItem("Content", null, LayoutAlignType.None, false);
            pContent.FillWidth = true;
            pContent.FillHeight = true;
            pContent.main = true;
        }

        public DockBoxState State
        {
            get { return state; }
            set
            {
                if (state != value)
                {
                    state = value;
                    if (state.HasFlag(DockBoxState.SizeLR))
                    {
                        Cursor = CursorType.ResizeLeftRight;
                    }
                    else if (state.HasFlag(DockBoxState.SizeUD))
                    {
                        Cursor = CursorType.ResizeUpDown;
                    }
                    else if (state.HasFlag(DockBoxState.Move))
                    {
                        Cursor = CursorType.Move;
                    }
                    else
                    {
                        Cursor = CursorType.Arrow;
                    }
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DockMap Map
        {
            get { return map; }
            set
            {
                if (map == value)
                    return;
                map = value;
            }
        }

        public DockMapItem PContent
        {
            get { return pContent; }
        }

        public IDockContainer DockParent
        {
            get { return null; }
        }

        protected override void OnButtonPressed(ButtonEventArgs args)
        {
            base.OnButtonPressed(args);
            if (args.Button == PointerButton.Left)
            {
                cach = args.Position;
                if (State.HasFlag(DockBoxState.SizeLR))
                {
                    State |= DockBoxState.InProcess;
                    stateSize = new Rectangle(hitLeft.Item.Bound.Right, hitLeft.Item.Bound.Top, 6, hitLeft.Item.Bound.Height);
                }
                else if (State.HasFlag(DockBoxState.SizeUD))
                {
                    State |= DockBoxState.InProcess;
                    stateSize = new Rectangle(hitTop.Item.Bound.Left, hitTop.Item.Bound.Bottom, hitTop.Item.Bound.Width, 6);
                }
            }
        }

        protected override void OnButtonReleased(ButtonEventArgs args)
        {
            base.OnButtonReleased(args);

            if (State.HasFlag(DockBoxState.InProcess))
            {
                if (State.HasFlag(DockBoxState.SizeLR))
                {
                    var dx = cach.X - args.Position.X;
                    if (hitLeft != null && hitLeft.Item != null)
                        hitLeft.Item.Width -= dx;
                    if (hitRight != null && hitRight.Item != null)
                        hitRight.Item.Width += dx;

                }
                if (State.HasFlag(DockBoxState.SizeUD))
                {
                    var dy = cach.Y - args.Position.Y;
                    if (hitTop != null && hitTop.Item != null)
                        hitTop.Item.Height -= dy;
                    if (hitBottom != null && hitBottom.Item != null)
                        hitBottom.Item.Height += dy;

                }
                if (State.HasFlag(DockBoxState.Move))
                {
                    var htest = DockHitTest(args.X, args.Y, 50);
                    page.List.Remove(page);
                    if (htest.Align == LayoutAlignType.None)
                    {
                        htest.Item.Panel.Pages.Items.Add(page);
                    }
                    else
                    {
                        DockMapItem nitem = GetDockItem(htest.Item.Name + htest.Align.ToString(), htest.Item, htest.Align, true);
                        nitem.Panel.Pages.Items.Add(page);
                    }
                    page = null;
                }
                QueueForReallocate();
                QueueDraw();
                State = DockBoxState.Default;
            }
        }

        protected override void OnMouseMoved(MouseMovedEventArgs args)
        {
            base.OnMouseMoved(args);
            if (State.HasFlag(DockBoxState.InProcess))
            {
                if (State.HasFlag(DockBoxState.SizeLR))
                {
                    stateSize.Location = new Point(args.Position.X, stateSize.Y);
                }
                else if (State.HasFlag(DockBoxState.SizeUD))
                {
                    stateSize.Location = new Point(stateSize.X, args.Position.Y);
                }
                else if (State.HasFlag(DockBoxState.Move))
                {
                    var htest = DockHitTest(args.X, args.Y, 50);
                    if (htest.Item != null)
                    {
                        stateMove = Rectangle.Zero;
                        if (htest.Align == LayoutAlignType.None)
                        {
                            stateMove = new Rectangle(htest.Item.Bound.X + 10, htest.Item.Bound.Y + 10, htest.Item.Bound.Width - 20, htest.Item.Bound.Height - 20);
                        }
                        else
                        {
                            stateMove = new Rectangle(htest.AlignBound.X + 10, htest.AlignBound.Y + 10, htest.AlignBound.Width - 20, htest.AlignBound.Height - 20);
                        }
                    }
                }
                QueueDraw();
            }
            else
            {
                var test = DockHitTest(args.X, args.Y, 1);
                if (test.Item == null)
                {
                    hitLeft = DockHitTest(args.X - 10, args.Y);
                    hitRight = DockHitTest(args.X + 10, args.Y);
                    hitTop = DockHitTest(args.X, args.Y - 10);
                    hitBottom = DockHitTest(args.X, args.Y + 10);

                    if (hitLeft.Item != null && hitRight.Item != null && hitLeft.Item != hitRight.Item)
                    {
                        State = DockBoxState.SizeLR;
                    }
                    else if (hitTop.Item != null && hitBottom.Item != null && hitTop.Item != hitBottom.Item)
                    {
                        State = DockBoxState.SizeUD;
                    }
                    else
                    {
                        State = DockBoxState.Default;
                    }
                }
                else
                {
                    State = DockBoxState.Default;
                }
            }
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            base.OnGetPreferredSize(widthConstraint, heightConstraint);
            foreach (DockMapItem item in LayoutMapHelper.GetVisibleItems(map))
            {
                var size = item.Panel.Surface.GetPreferredSize();
                item.Width = size.Width;
                item.Height = size.Height;
            }
            map.GetBound();
            return map.Bound.Size;
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            map.GetBound(Size.Width, Size.Height);
            foreach (DockMapItem item in LayoutMapHelper.GetVisibleItems(map))
            {
                map.GetBound(item);
                item.Bound = item.Bound.Inflate(-3, -3);
                if (item.Bound.Width > 0 && item.Bound.Height > 0)
                {
                    SetChildBounds(item.Panel, item.Bound);
                }
            }
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);
            if (State.HasFlag(DockBoxState.InProcess))
            {
                ctx.SetColor(Colors.Black);
                ctx.Rectangle(stateSize);
                ctx.Stroke();
            }
            //GraphContext cont = new GraphContext(e.Graphics);
            //List<ILayoutMapItem> list = LayoutMapTool.GetVisibleItems(map);
            //Pen p = new Pen(SystemBrushes.WidgetDarkDark, 6);
            //foreach (DockMapItem item in list)
            //{
            //RectangleF rect = map.GetBound(item);

            //rect.X++;
            //rect.Y++;
            //rect.Width -= 1;
            //rect.Height -= 1;

            //cont.G.DrawRectangle(p, rect.X, rect.Y, rect.Width, rect.Height);

            //rect = item.Panel.Pages.Bounds;
            //rect.Width += 5;

            //cont.G.FillRectangle(SystemBrushes.WidgetDarkDark, rect);
            //}
        }

        public void Localizing()
        {
            foreach (Widget control in GetControls())
            {
                var loc = control as ILocalizable;
                if (loc != null)
                    loc.Localize();
            }
        }

        public DockPage PickTool(Widget control)
        {
            return PickTool(this, control);
        }

        public DockPage PickTool(IDockContainer cont, Widget control)
        {
            foreach (IDockContainer idc in cont.GetDocks())
            {
                if (idc.Contains(control))
                {
                    if (idc is DockPanel)
                        return ((DockPanel)idc).GetPage(control);
                    else
                        return PickTool(idc, control);
                }
            }
            return null;
        }

        public static DockPage CreatePage(Widget control)
        {
            var page = new DockPage();

            if (control is IDockContent)
                page.HideOnClose = ((IDockContent)control).HideOnClose;

            page.Name = control.Name;
            page.Widget = control;
            return page;
        }

        public DockBoxHitTest DockHitTest(double x, double y)
        {
            return DockHitTest(x, y, 3);
        }

        public DockBoxHitTest DockHitTest(double x, double y, double size)
        {
            var htest = new DockBoxHitTest();
            foreach (DockMapItem item in LayoutMapHelper.GetVisibleItems(map))
            {
                if (item.Bound.Contains(x, y))
                {
                    htest.Item = item;
                    htest.Bound = item.Bound;
                    break;
                }
            }
            if (htest.Item != null)
            {
                var rect = Rectangle.Zero;
                htest.Align = GuiService.GetAlignRect(htest.Item.Bound, size, x, y, ref rect);
                htest.AlignBound = rect;
            }
            return htest;
        }

        private void MapPageSelected(object sender, DockPageEventArgs arg)
        {
            if (childFocusHandler != null && arg.Page != null)
                childFocusHandler(arg.Page.Widget, arg);
        }

        public DockPage PickPage(int x, int y, DockMapItem item)
        {
            foreach (DockPage itemPage in item.Panel.Pages.Items)
            {
                if (itemPage.Bound.Contains(x, y))
                {
                    return itemPage;
                }
            }
            return null;
        }

        public event EventHandler ContentFocus
        {
            add { childFocusHandler += value; }
            remove { childFocusHandler -= value; }
        }

        private void ChildFocusInEvent(object o, EventArgs args)
        {
            if (childFocusHandler != null)
                childFocusHandler(o, args);
        }

        private void Pages_PageClick(object sender, DockPageEventArgs e)
        {
            ChildFocusInEvent(e.Page.Widget, e);
        }

        private void PanelTabSelected(object sender, DockPageEventArgs e)
        {

            if (e.Page == null)
            {
                if (!((DockPanel)sender).MapItem.FillWidth)
                {
                    ((DockPanel)sender).MapItem.Visible = false;
                    QueueDraw();
                }
            }
            else if (!((DockPanel)sender).MapItem.Visible)
            {
                ((DockPanel)sender).MapItem.Visible = true;
                QueueDraw();
            }

            if (PageSelected != null)
                PageSelected(this, e);
        }

        private void OnPageDrag(object sender, DockPageEventArgs e)
        {
            page = e.Page;
        }

        public DockMapItem GetDockItem(string Name, LayoutAlignType type, bool gp)
        {
            return GetDockItem(Name, pContent, type, gp);
        }

        public DockMapItem GetDockItem(string Name, DockMapItem exitem, LayoutAlignType type, bool gp)
        {
            DockMapItem item = LayoutMapHelper.Get(map, Name) as DockMapItem;
            if (item == null)
            {
                item = CreateDockItem(Name);
                if (Name == "Right" || Name == "Left")
                    item.FillHeight = true;
                if (Name == "Bottom")
                    item.Height = 200;
                if (exitem == null)
                    LayoutMapHelper.Add(map, item);
                else
                    LayoutMapHelper.InsertWith(item, exitem, type, gp);

            }
            return item;
        }

        public DockMapItem CreateDockItem(string Name)
        {
            var item = new DockMapItem();
            item.Name = Name;
            item.Visible = true;
            item.Panel = new DockPanel();
            item.Panel.PageSelected += PanelTabSelected;
            item.Panel.Pages.PageClick += Pages_PageClick;
            item.Panel.Pages.PageDrag += OnPageDrag;
            AddChild(item.Panel);
            return item;
        }

        #region Container
        public bool Contains(Widget c)
        {
            foreach (DockMapItem item in LayoutMapHelper.GetItems(map))
                if (item.Panel.Contains(c))
                    return true;
            return false;
        }

        public bool Delete(Widget c)
        {
            foreach (DockMapItem item in LayoutMapHelper.GetItems(map))
            {
                if (item.Panel.Contains(c))
                {
                    item.Panel.Delete(c);
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<Widget> GetControls()
        {
            foreach (DockMapItem item in LayoutMapHelper.GetItems(map))
            {
                foreach (var widget in item.Panel.GetControls())
                    yield return widget;
            }
        }

        public IEnumerable<DockPage> GetPages()
        {
            foreach (DockMapItem item in LayoutMapHelper.GetItems(map))
            {
                foreach (var pageItem in item.Panel.Pages.Items)
                    yield return pageItem;
            }
        }

        public DockPage GetPage(Widget c)
        {
            foreach (DockMapItem item in LayoutMapHelper.GetItems(map))
            {
                DockPage dp = item.Panel.GetPage(c);
                if (dp != null)
                {
                    return dp;
                }
            }
            return null;
        }

        public Widget Find(string name)
        {
            foreach (Widget content in GetControls())
            {
                if (content.Name == name)
                {
                    return content;
                }
            }
            return null;
        }

        public IEnumerable<IDockContainer> GetDocks()
        {
            foreach (DockMapItem item in LayoutMapHelper.GetVisibleItems(map))
            {
                yield return item.Panel;
            }
        }

        public DockPage Put(Widget c)
        {
            var docktype = c is IDockContent ? ((IDockContent)c).DockType : DockType.Content;
            return Put(c, docktype);
        }

        public DockPage Put(Widget widget, DockType type)
        {
            DockPage page = PickTool(widget);
            if (page != null)
            {
                page.Panel.SelectPage(page);
            }
            else
            {
                widget.GotFocus += ChildFocusInEvent;

                DockMapItem item = null;
                if (type == DockType.Content)
                {
                    item = pContent;
                }
                else if (type == DockType.Left)
                {
                    item = GetDockItem("Left", pContent, LayoutAlignType.Left, false);
                }
                else if (type == DockType.LeftBottom)
                {
                    item = GetDockItem("Left", pContent, LayoutAlignType.Left, false);
                    item = GetDockItem("LeftBottom", item, LayoutAlignType.Bottom, true);
                }
                else if (type == DockType.Right)
                {
                    item = GetDockItem("Right", pContent, LayoutAlignType.Right, false);
                }
                else if (type == DockType.RightBottom)
                {
                    item = GetDockItem("Right", pContent, LayoutAlignType.Right, false);
                    item = GetDockItem("RightBottom", item, LayoutAlignType.Bottom, true);
                }
                else if (type == DockType.Top)
                {
                    item = GetDockItem("Top", pContent, LayoutAlignType.Top, false);
                }
                else if (type == DockType.Bottom)
                {
                    item = GetDockItem("Bottom", pContent, LayoutAlignType.Bottom, true);
                    //item.Panel.PagesAlign = LayoutAlignType.Bottom;
                }
                item.Visible = true;
                page = item.Panel.Put(widget);
            }
            QueueForReallocate();
            return page;
        }
        #endregion

        public void ClosePage(Widget c)
        {
            DockPage dp = GetPage(c);
            if (dp != null)
            {
                dp.Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var item in map.Items)
                if (item is IDisposable)
                    ((IDisposable)item).Dispose();
            base.Dispose(disposing);
        }
    }



}