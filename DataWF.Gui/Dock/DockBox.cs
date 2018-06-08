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

    public class DockBox : Canvas, IDockContainer, ISerializableElement
    {
        private DockItem items;
        private DockPage page = null;

        private DockBoxState state = DockBoxState.Default;
        private Point cach;
        private Rectangle stateMove;
        private Rectangle stateSize;
        private DockBoxHitTest hitLeft = null;
        private DockBoxHitTest hitRight = null;
        private DockBoxHitTest hitTop = null;
        private DockBoxHitTest hitBottom = null;
        private bool visibleClose = true;
        private DockItem content;
        private Image processImage;
        private List<DockItem> hided = new List<DockItem>();

        public event EventHandler<DockPageEventArgs> PageSelected;

        public DockBox() : base()
        {
            Items = new DockItem() { };
        }

        public DockBox(params DockItem[] items) : base()
        {
            Items = new DockItem() { };
            foreach (var item in items)
            {
                Add(item);
            }
        }

        private DockItem Main
        {
            get
            {
                if (content == null)
                {
                    content = GetDockItem("Content", null, LayoutAlignType.None, false);
                    content.FillWidth = true;
                    content.FillHeight = true;
                    content.Main = true;
                }
                return content;
            }
            set { }
        }

        public DockBoxState State
        {
            get { return state; }
            set
            {
                if (state != value)
                {
                    if (value.HasFlag(DockBoxState.InProcess))
                    {
                        processImage = Toolkit.CurrentEngine.RenderWidget(this);
                        //processImage.Save("test.png", ImageFileType.Png);
                        foreach (var widget in Children)
                        {
                            if (widget.Visible)
                            {
                                widget.Visible = false;
                            }
                        }
                    }
                    else if (state.HasFlag(DockBoxState.InProcess))
                    {
                        processImage?.Dispose();
                        foreach (var item in items.GetVisibleItems())
                        {
                            if (item.Panel != null)
                            {
                                item.Panel.Visible = true;
                            }
                        }
                    }
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
                    QueueDraw();
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DockItem Items
        {
            get { return items; }
            set
            {
                if (items == value)
                    return;
                items = value;
                items.DockBox = this;
            }
        }

        public IDockContainer DockParent
        {
            get { return null; }
        }

        public bool VisibleClose
        {
            get { return visibleClose; }
            set
            {
                visibleClose = value;
                foreach (var panel in GetDockPanels())
                {
                    panel.VisibleClose = value;
                }
            }
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
                    page.Remove();
                    if (htest.Item != null)
                    {
                        if (htest.Align == LayoutAlignType.None)
                        {
                            htest.Item.Panel.Put(page);
                        }
                        else
                        {
                            var name = $"{(htest.Item.Name == "Content" ? "" : htest.Item.Name)}{htest.Align.ToString()}";
                            DockItem nitem = GetDockItem(name, htest.Item, htest.Align, true);
                            nitem.Panel.Put(page);
                        }
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
                            stateMove = htest.Item.Bound.Inflate(-40, -40);
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

        public void Localize()
        {
            foreach (Widget control in GetControls())
            {
                var loc = control as ILocalizable;
                if (loc != null)
                    loc.Localize();
            }
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            foreach (DockItem item in items.GetVisibleItems())
            {
                item.Panel?.Surface.GetPreferredSize();
            }
            return items.GetBound(0, 0).Size;
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            var mapBound = items.GetBound(Size.Width, Size.Height);
            foreach (DockItem item in items.GetVisibleItems())
            {
                items.GetBound(item, mapBound);
                if (item.Panel != null && item.Bound.Width > 0 && item.Bound.Height > 0)
                {
                    SetChildBounds(item.Panel, item.Bound);
                }
            }
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);
            using (var cont = new GraphContext(ctx))
            {
                cont.FillRectangle(GuiEnvironment.Theme["Dock"].BackBrush.Color, this.Bounds);

                if (State.HasFlag(DockBoxState.InProcess))
                {
                    cont.DrawImage(processImage, new Rectangle(Point.Zero, processImage.Size));
                    if (state.HasFlag(DockBoxState.Move))
                    {
                        cont.DrawCell(GuiEnvironment.Theme["Selection"], null, stateMove, stateMove, CellDisplayState.Default);
                    }
                    else
                    {
                        cont.DrawCell(GuiEnvironment.Theme["Selection"], null, stateSize, stateSize, CellDisplayState.Default);
                    }
                }
            }
        }
        public DockPage PickPage(Widget control)
        {
            return PickPage(this, control);
        }

        public DockPage PickPage(IDockContainer cont, Widget control)
        {
            foreach (IDockContainer idc in cont.GetDocks())
            {
                if (idc.Contains(control))
                {
                    if (idc is DockPanel)
                        return ((DockPanel)idc).GetPage(control);
                    else
                        return PickPage(idc, control);
                }
            }
            return null;
        }

        public DockBoxHitTest DockHitTest(double x, double y)
        {
            return DockHitTest(x, y, 3);
        }

        public DockBoxHitTest DockHitTest(double x, double y, double size)
        {
            var htest = new DockBoxHitTest();
            foreach (DockItem item in items.GetVisibleItems())
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
            if (ContentFocus != null && arg.Page != null)
                ContentFocus(arg.Page.Widget, arg);
        }

        public DockPage PickPage(int x, int y, DockItem item)
        {
            foreach (DockPage itemPage in item.Panel.Items)
            {
                if (itemPage.Bound.Contains(x, y))
                {
                    return itemPage;
                }
            }
            return null;
        }

        public event EventHandler ContentFocus;

        private void ChildFocusInEvent(object sender, EventArgs args)
        {
            ContentFocus?.Invoke(sender, args);
        }

        public void HideExcept(DockItem item)
        {
            foreach (var dockItem in Items)
            {
                if (dockItem != item && dockItem.Visible)
                {
                    hided.Add(dockItem);
                    dockItem.Visible = false;
                }
            }
        }

        public void Unhide()
        {
            foreach (var dockItem in hided)
            {
                dockItem.Visible = true;
            }
            hided.Clear();
        }

        protected internal void OnPageDoubleClick(DockPage page)
        {
            if (hided.Count == 0)
            {
                HideExcept(page.Panel.DockItem);
            }
            else
            {
                Unhide();
            }
        }

        protected internal void OnPageSelected(DockPanel panel, DockPageEventArgs e)
        {
            if (e.Page == null)
            {
                if (!panel.DockItem.Main)
                {
                    panel.DockItem.Visible = false;
                }
                Unhide();
            }
            else if (!panel.DockItem.Visible)
            {
                panel.DockItem.Visible = true;
            }

            QueueForReallocate();
            ChildFocusInEvent(panel, e);
            PageSelected?.Invoke(this, e);
        }

        protected internal void OnPageDrag(ToolItemEventArgs e)
        {
            page = e.Item as DockPage;
            State = DockBoxState.Move | DockBoxState.InProcess;
        }

        public DockItem GetDockItem(string name, LayoutAlignType type, bool gp)
        {
            return GetDockItem(name, Main, type, gp);
        }

        public DockItem GetDockItem(string name, DockItem exist, LayoutAlignType type, bool gp)
        {
            DockItem item = items.GetRecursive(name) as DockItem;
            if (item == null)
            {
                item = CreateDockItem(name, exist, type, gp);
                Add(item, exist, type, gp);
            }
            return item;
        }

        public DockItem CreateDockItem(string name, DockItem exist, LayoutAlignType type, bool gp)
        {
            var item = new DockItem()
            {
                Name = name,
                Visible = true,
                FillHeight = name == "Right" || name == "Left",
                Panel = new DockPanel()
            };
            if (name == "Bottom")
                item.Height = 200;
            return item;
        }

        public void Add(DockItem item, DockItem exist = null, LayoutAlignType type = LayoutAlignType.None, bool gp = false)
        {
            if (exist == null)
            {
                items.Add(item);
            }
            else
            {
                exist.InsertWith(item, type, gp);
            }
        }

        internal void Add(DockPanel panel)
        {
            panel.VisibleClose = VisibleClose;
            AddChild(panel);
        }

        internal void Remove(DockPanel panel)
        {
            RemoveChild(panel);
        }

        #region Container
        public bool Contains(Widget c)
        {
            foreach (DockItem item in items.GetItems())
                if (item.Panel.Contains(c))
                    return true;
            return false;
        }

        public bool Delete(Widget c)
        {
            foreach (DockItem item in items.GetItems())
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
            foreach (DockItem item in items.GetItems())
            {
                foreach (var widget in item.Panel.GetControls())
                    yield return widget;
            }
        }

        public IEnumerable<DockPage> GetPages()
        {
            foreach (DockItem item in items.GetItems())
            {
                foreach (var pageItem in item.Panel)
                    yield return pageItem;
            }
        }

        public DockPage GetPage(string name)
        {
            foreach (DockItem item in items.GetItems())
            {
                DockPage dp = item.Panel.GetPage(name);
                if (dp != null)
                {
                    return dp;
                }
            }
            return null;
        }

        public DockPage GetPage(Widget c)
        {
            foreach (DockItem item in items.GetItems())
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
            foreach (DockItem item in items.GetVisibleItems())
            {
                yield return item.Panel;
            }
        }

        public IEnumerable<DockPanel> GetDockPanels()
        {
            foreach (DockItem item in items.GetVisibleItems())
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
            DockPage page = PickPage(widget);
            if (page != null)
            {
                page.Panel.CurrentPage = page;
            }
            else
            {
                widget.GotFocus += ChildFocusInEvent;

                DockItem item = null;
                if (type == DockType.Content)
                {
                    item = Main;
                }
                else if (type == DockType.Left)
                {
                    item = GetDockItem("Left", Main, LayoutAlignType.Left, false);
                }
                else if (type == DockType.LeftBottom)
                {
                    item = GetDockItem("Left", Main, LayoutAlignType.Left, false);
                    item = GetDockItem("LeftBottom", item, LayoutAlignType.Bottom, true);
                }
                else if (type == DockType.Right)
                {
                    item = GetDockItem("Right", Main, LayoutAlignType.Right, false);
                }
                else if (type == DockType.RightBottom)
                {
                    item = GetDockItem("Right", Main, LayoutAlignType.Right, false);
                    item = GetDockItem("RightBottom", item, LayoutAlignType.Bottom, true);
                }
                else if (type == DockType.Top)
                {
                    item = GetDockItem("Top", Main, LayoutAlignType.Top, false);
                }
                else if (type == DockType.Bottom)
                {
                    item = GetDockItem("Bottom", Main, LayoutAlignType.Bottom, false);
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
            Application.Invoke(() => items.Dispose());
            base.Dispose(disposing);
        }

        public void Serialize(ISerializeWriter writer)
        {
            writer.Write(items);
        }

        public void Deserialize(ISerializeReader reader)
        {
            reader.Read(items);
        }
    }



}