using DataWF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

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

    public class DockBox : Canvas, IDockContainer, IXMLSerializable
    {
        private DockItem items;
        private DockPage page = null;

        private DockBoxState state = DockBoxState.Default;
        private Point cach;
        private Rect stateMove;
        private Rect stateSize;
        private DockBoxHitTest hitLeft = null;
        private DockBoxHitTest hitRight = null;
        private DockBoxHitTest hitTop = null;
        private DockBoxHitTest hitBottom = null;
        private bool visibleClose = true;
        private DockItem content;
        private Bitmap processImage;
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
            get => state;
            set
            {
                if (state != value)
                {
                    if (value.HasFlag(DockBoxState.InProcess))
                    {
                        processImage = Toolkit.CurrentEngine.RenderControl(this);
                        //processImage.Save("test.png", ImageFileType.Png);
                        foreach (var Control in Children)
                        {
                            if (Control.IsVisible)
                            {
                                Control.IsVisible = false;
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
                                item.Panel.IsVisible = true;
                            }
                        }
                    }
                    state = value;
                    if (state.HasFlag(DockBoxState.SizeLR))
                    {
                        Cursor = StandardCursorType.SizeWestEast;
                    }
                    else if (state.HasFlag(DockBoxState.SizeUD))
                    {
                        Cursor = StandardCursorType.SizeNorthSouth;
                    }
                    else if (state.HasFlag(DockBoxState.Move))
                    {
                        Cursor = StandardCursorType.DragMove;
                    }
                    else
                    {
                        Cursor = StandardCursorType.Arrow;
                    }
                    InvalidateVisual();
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DockItem Items
        {
            get => items;
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
            get => visibleClose;
            set
            {
                visibleClose = value;
                foreach (var panel in GetDockPanels())
                {
                    panel.VisibleClose = value;
                }
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs args)
        {
            base.OnPointerPressed(args);
            if (args.MouseButton == MouseButton.Left)
            {
                cach = args.GetPosition(this);
                if (State.HasFlag(DockBoxState.SizeLR))
                {
                    State |= DockBoxState.InProcess;
                    stateSize = new Rect(hitLeft.Item.Bound.Right, hitLeft.Item.Bound.Top, 6, hitLeft.Item.Bound.Height);
                }
                else if (State.HasFlag(DockBoxState.SizeUD))
                {
                    State |= DockBoxState.InProcess;
                    stateSize = new Rect(hitTop.Item.Bound.Left, hitTop.Item.Bound.Bottom, hitTop.Item.Bound.Width, 6);
                }
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs args)
        {
            base.OnPointerReleased(args);
            var position = args.GetPosition(this);
            if (State.HasFlag(DockBoxState.InProcess))
            {
                if (State.HasFlag(DockBoxState.SizeLR))
                {
                    var dx = cach.X - position.X;
                    if (hitLeft != null && hitLeft.Item != null)
                        hitLeft.Item.Width -= dx;
                    if (hitRight != null && hitRight.Item != null)
                        hitRight.Item.Width += dx;

                }
                if (State.HasFlag(DockBoxState.SizeUD))
                {
                    var dy = cach.Y - position.Y;
                    if (hitTop != null && hitTop.Item != null)
                        hitTop.Item.Height -= dy;
                    if (hitBottom != null && hitBottom.Item != null)
                        hitBottom.Item.Height += dy;

                }
                if (State.HasFlag(DockBoxState.Move))
                {
                    var htest = DockHitTest(position, 50);
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
                InvalidateArrange();
                InvalidateVisual();
                State = DockBoxState.Default;
            }
        }

        protected override void OnPointerMoved(PointerEventArgs args)
        {
            base.OnPointerMoved(args);
            var position = args.GetPosition(this);
            if (State.HasFlag(DockBoxState.InProcess))
            {
                if (State.HasFlag(DockBoxState.SizeLR))
                {
                    stateSize = new Rect(new Point(position.X, stateSize.Y), stateSize.Size);
                }
                else if (State.HasFlag(DockBoxState.SizeUD))
                {
                    stateSize = new Rect(new Point(stateSize.X, position.Y), stateSize.Size);
                }
                else if (State.HasFlag(DockBoxState.Move))
                {
                    var htest = DockHitTest(position, 50);
                    if (htest.Item != null)
                    {
                        stateMove = Rect.Empty;
                        if (htest.Align == LayoutAlignType.None)
                        {
                            stateMove = htest.Item.Bound.Inflate(new Thickness(-40, -40));
                        }
                        else
                        {
                            stateMove = new Rect(htest.AlignBound.X + 10, htest.AlignBound.Y + 10, htest.AlignBound.Width - 20, htest.AlignBound.Height - 20);
                        }
                    }
                }
                InvalidateVisual();
            }
            else
            {
                var test = DockHitTest(position, 1);
                if (test.Item == null)
                {
                    hitLeft = DockHitTest(position.WithX(position.X - 10));
                    hitRight = DockHitTest(position.WithX(position.X + 10));
                    hitTop = DockHitTest(position.WithY(position.Y - 10));
                    hitBottom = DockHitTest(position.WithY(position.Y + 10));

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
            foreach (Control control in GetControls())
            {
                if (control is ILocalizable loc)
                {
                    loc.Localize();
                }
            }
        }

        protected override Size MeasureOverride(Size aSize)
        {
            foreach (DockItem item in items.GetVisibleItems())
            {
                item.Panel?.Measure(aSize);
            }
            return items.GetBound(0, 0).Size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var mapBound = items.GetBound(finalSize.Width, finalSize.Height);
            foreach (DockItem item in items.GetVisibleItems())
            {
                items.GetBound(item, mapBound);
                if (item.Panel != null && item.Bound.Width > 0 && item.Bound.Height > 0)
                {
                    item.Panel.Arrange(item.Bound);
                }
            }
            return finalSize;
        }

        public override void Render(DrawingContext ctx)
        {
            base.Render(ctx);
            var dirtyRect = Bounds;
            using (var cont = new GraphContext(ctx))
            {
                cont.FillRectangle(GuiEnvironment.Theme["Dock"].BackBrush.Color, this.Bounds);

                if (State.HasFlag(DockBoxState.InProcess))
                {
                    cont.DrawImage(processImage, new Rect(default(Point), processImage.Size));
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
        public DockPage PickPage(Control control)
        {
            return PickPage(this, control);
        }

        public DockPage PickPage(IDockContainer cont, Control control)
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

        public DockBoxHitTest DockHitTest(Point point)
        {
            return DockHitTest(point, 3);
        }

        public DockBoxHitTest DockHitTest(Point point, double size)
        {
            var htest = new DockBoxHitTest();
            foreach (DockItem item in items.GetVisibleItems())
            {
                if (item.Bound.Contains(point))
                {
                    htest.Item = item;
                    htest.Bound = item.Bound;
                    break;
                }
            }
            if (htest.Item != null)
            {
                var rect = Rect.Empty;
                htest.Align = GuiService.GetAlignRect(htest.Item.Bound, size, point, ref rect);
                htest.AlignBound = rect;
            }
            return htest;
        }

        private void MapPageSelected(object sender, DockPageEventArgs arg)
        {
            if (ContentFocus != null && arg.Page != null)
                ContentFocus(arg.Page.Control, arg);
        }

        public DockPage PickPage(Point point, DockItem item)
        {
            foreach (DockPage itemPage in item.Panel.Items)
            {
                if (itemPage.Bound.Contains(point))
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

            InvalidateArrange();
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
            if (!(items.GetRecursive(name) is DockItem item))
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
            Children.Add(panel);
        }

        internal void Remove(DockPanel panel)
        {
            Children.Remove(panel);
        }

        #region Container
        public bool Contains(Control c)
        {
            foreach (DockItem item in items.GetItems())
                if (item.Panel.Contains(c))
                    return true;
            return false;
        }

        public bool Delete(Control c)
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

        public IEnumerable<IControl> GetControls()
        {
            foreach (DockItem item in items.GetItems())
            {
                foreach (var Control in item.Panel.GetControls())
                    yield return Control;
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

        public DockPage GetPage(Control c)
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

        public Control Find(string name)
        {
            foreach (Control content in GetControls())
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
            foreach (DockItem item in items.GetItems())
            {
                yield return item.Panel;
            }
        }

        public IEnumerable<DockPanel> GetDockPanels()
        {
            foreach (DockItem item in items.GetItems())
            {
                yield return item.Panel;
            }
        }

        public DockPage Put(Control c)
        {
            var docktype = c is IDockContent ? ((IDockContent)c).DockType : DockType.Content;
            return Put(c, docktype);
        }

        public DockPage Put(Control Control, DockType type)
        {
            DockPage page = PickPage(Control);
            if (page != null)
            {
                page.Panel.DockItem.Visible = true;
                if (page.Visible)
                    page.Panel.CurrentPage = page;
                else
                    page.Visible = true;
            }
            else
            {
                Control.GotFocus += ChildFocusInEvent;

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
                page = item.Panel.Put(Control);
            }
            InvalidateArrange();
            return page;
        }
        #endregion

        public void ClosePage(Control c)
        {
            DockPage dp = GetPage(c);
            if (dp != null)
            {
                dp.Close();
            }
        }

        protected void Dispose(bool disposing)
        {
            _ = Dispatcher.UIThread.InvokeAsync(() => items.Dispose());
        }

        public void Serialize(XmlInvokerWriter writer)
        {
            writer.Write(items);
        }

        public void Deserialize(XmlInvokerReader reader)
        {
            reader.Read(items);
        }

        public bool Contains(IControl control)
        {
            return Children.Contains(control);
        }

        public bool Delete(IControl control)
        {
            return Children.Remove(control);
        }

        public DockPage Put(IControl control)
        {
            throw new NotImplementedException();
        }

        public DockPage Put(IControl control, DockType type)
        {
            throw new NotImplementedException();
        }

        IEnumerable<IControl> IDockContainer.GetControls()
        {
            throw new NotImplementedException();
        }

        IControl IDockContainer.Find(string name)
        {
            throw new NotImplementedException();
        }
    }



}