using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class DockPanel : Toolsbar, IEnumerable, IEnumerable<DockPage>, IDockContainer, ILocalizable, ISerializableElement
    {
        private DockPage currentPage;
        //private Menubar context;
        //private ToolMenuItem toolHide;
        private LayoutAlignType pagesAlign = LayoutAlignType.Top;
        private LinkedList<DockPage> pagesHistory = new LinkedList<DockPage>();

        private Widget widget;
        private Orientation orientation = Orientation.Horizontal;
        private bool viewClose = true;
        private Rectangle widgetBounds;
        private TextLayout selectText;

        public DockPanel() : base()
        {
            Items.StyleName = "Window";
            Indent = 1;
            //context = new Menubar(toolHide);
            //toolHide = new ToolMenuItem { Name = "Hide", Text = "Hide" };

            Name = nameof(DockPanel);
        }

        public DockPanel(params Widget[] widgets) : this()
        {
            foreach (var widget in widgets)
            {
                Put(widget);
            }
        }

        public DockItem DockItem { get; set; }

        public DockBox DockBox { get { return DockItem?.DockBox; } }

        public Widget CurrentWidget
        {
            get { return widget; }
            internal set
            {
                if (value == widget)
                    return;

                if (widget != null)
                {
                    RemoveChild(widget);
                }

                widget = value;

                if (widget != null)
                {
                    AddChild(widget);
                    SetChildBounds(widget, widgetBounds);
                }
            }
        }

        public LayoutAlignType PagesAlign
        {
            get { return pagesAlign; }
            set
            {
                if (pagesAlign == value)
                    return;
                pagesAlign = value;
                if (pagesAlign == LayoutAlignType.Left ||
                    pagesAlign == LayoutAlignType.Right)
                    Orientation = Orientation.Vertical;
                else
                    Orientation = Orientation.Horizontal;
                //PerformLayout();
            }
        }

        public void RemovePage(DockPage page)
        {
            if (page == null)
                return;
            while (pagesHistory.Remove(page))
            { }
            DockPage npage = null;

            if (CurrentPage == page)
            {
                var item = pagesHistory.Last;

                while (item != null && (item.Value == page || item.Value.Panel != this || !item.Value.Visible))
                    item = item.Previous;
                if (item != null && item.Value.Panel == this && item.Value.Visible)
                    npage = item.Value;
                else
                    npage = (DockPage)items.GetVisibleItems().FirstOrDefault(p => p != page);
            }
            else
            {
                npage = CurrentPage;
            }
            CurrentPage = npage;
            QueueDraw();
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            double def = 30;
            var pagesRect = Rectangle.Zero;
            widgetBounds = Rectangle.Zero;
            if (pagesAlign == LayoutAlignType.Top)
            {
                if (ItemOrientation == Orientation.Vertical)
                    def = 100D;
                pagesRect = new Rectangle(0D, 0D, Size.Width, def);
                widgetBounds = new Rectangle(0D, def, Size.Width, Size.Height - def);
            }
            else if (pagesAlign == LayoutAlignType.Bottom)
            {
                if (ItemOrientation == Orientation.Vertical)
                    def = 100D;

                pagesRect = new Rectangle(0D, Size.Height - def, Size.Width, def);
                widgetBounds = new Rectangle(0D, 0D, Size.Width, Size.Height - def);
            }
            else if (pagesAlign == LayoutAlignType.Left)
            {
                if (ItemOrientation == Orientation.Horizontal)
                    def = 100;

                pagesRect = new Rectangle(0D, 0D, def, Size.Height);
                widgetBounds = new Rectangle(def, 0D, Size.Width - def, Size.Height);
            }
            else if (pagesAlign == LayoutAlignType.Right)
            {
                if (ItemOrientation == Orientation.Horizontal)
                    def = 100;

                pagesRect = new Rectangle(Size.Width - def, 0D, def, Size.Height);
                widgetBounds = new Rectangle(0D, 0D, Size.Width - def, Size.Height);
            }
            if (CurrentWidget != null)
            {
                SetChildBounds(CurrentWidget, widgetBounds);
            }
        }

        public void ClearPages()
        {
            Items.Clear();
            CurrentPage = null;
        }

        public void SelectPageByControl(Widget control)
        {
            var page = GetPage(control);
            if (page != null)
            {
                CurrentPage = page;
            }
        }

        public DockPage CurrentPage
        {
            get { return currentPage; }
            set
            {
                if (currentPage == value)
                    return;

                currentPage = value;
                if (currentPage != null)
                {
                    if (DockItem != null && !DockItem.Visible)
                    {
                        DockItem.Visible = true;
                    }
                    pagesHistory.AddLast(currentPage);
                    currentPage.Checked = true;
                    CurrentWidget = currentPage.Widget;
                }
                else
                {
                    CurrentWidget = null;
                }
                DockBox?.OnPageSelected(this, new DockPageEventArgs(value));
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetItems().GetEnumerator();
        }

        public IEnumerator<DockPage> GetEnumerator()
        {
            return Items.GetItems().Cast<DockPage>().GetEnumerator();
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

        public event EventHandler<ToolItemEventArgs> PageDrag;

        protected void OnPageDrag(ToolItemEventArgs arg)
        {
            PageDrag?.Invoke(this, arg);
            DockBox?.OnPageDrag(arg);
        }

        protected internal override void OnItemMove(ToolItemEventArgs args)
        {
            base.OnItemMove(args);
            OnPageDrag(args);
        }

        public event EventHandler<DockPageEventArgs> PageClose;

        public void OnPageClose(DockPage page)
        {
            if (page.Widget is IDockContent)
            {
                if (!((IDockContent)page.Widget).Closing())
                    return;
            }
            PageClose?.Invoke(this, new DockPageEventArgs(page));
            if (page.HideOnClose)
            {
                page.Visible = false;
            }
            else
            {
                Items.Remove(page);
                page.Widget.Dispose();
            }
        }

        protected override void OnDraw(GraphContext context)
        {
            base.OnDraw(context);
            Rectangle brect = orientation == Orientation.Horizontal
                ? new Rectangle(0, 30 - 5, Bounds.Width, 5)
                : new Rectangle(Bounds.Width - 5, 0, 5, 30);
            if (PagesAlign == LayoutAlignType.Bottom)
                brect.Y = 0;
            context.FillRectangle(items.Style, brect, CellDisplayState.Selected);//st.BackBrush.GetBrush(rectb, 

            if (CurrentWidget == null && widgetBounds.Width > 0 && widgetBounds.Height > 0)
            {
                if (selectText == null)
                {
                    selectText = new TextLayout
                    {
                        Font = Font.WithScaledSize(5D),
                        TextAlignment = Alignment.Center,
                        Text = "Select Document",
                    };
                }
                var bound = new Rectangle(widgetBounds.X, widgetBounds.Height / 2D, widgetBounds.Width, 100);
                context.DrawText(selectText, bound, items.Style.FontBrush.Color.WithAlpha(0.3));
            }
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            var baseSize = base.OnGetPreferredSize(widthConstraint, heightConstraint);
            if (CurrentWidget != null)
            {
                var size = CurrentWidget.Surface.GetPreferredSize();
                size.Height += baseSize.Height;
                return size;
            }
            return baseSize;
        }

        public override void OnItemClick(ToolItem item)
        {
            base.OnItemClick(item);
            if (!item.Checked)
            {
                CurrentPage = (DockPage)item;
            }
        }

        public override void OnItemDoubleClick(ToolItem item)
        {
            base.OnItemDoubleClick(item);
            DockBox?.OnPageDoubleClick((DockPage)item);
        }

        #region IDockContainer implementation

        public IDockContainer DockParent
        {
            get { return GuiService.GetDockParent(this); }
        }

        public bool Contains(Widget control)
        {
            foreach (DockPage t in Items)
                if (t.Widget == control)
                    return true;
            return false;
        }

        public IEnumerable<Widget> GetControls()
        {
            foreach (DockPage t in Items)
                yield return t.Widget;
        }

        public Widget Find(string name)
        {
            foreach (DockPage page in Items)
                if (page.Widget.Name == name)
                    return page.Widget;
            return null;
        }

        public DockPage Put(Widget widget)
        {
            return Put(widget, DockType.Content);
        }

        public DockPage Put(Widget widget, DockType type)
        {
            var page = new DockPage
            {
                Name = widget.Name,
                Widget = widget,
            };
            Put(page);
            return page;
        }

        public void Put(DockPage page)
        {
            page.Column = -1;
            page.Row = -1;
            Items.Add(page);
            CurrentPage = page;
        }

        public DockPage GetPage(string name)
        {
            foreach (DockPage page in Items)
                if (page.Widget?.Name == name)
                {
                    return page;
                }
            return null;
        }

        public DockPage GetPage(Widget control)
        {
            foreach (DockPage page in Items)
                if (page.Widget == control)
                {
                    return page;
                }
            return null;
        }

        public bool Delete(Widget control)
        {
            var page = GetPage(control);
            if (page != null)
            {
                page.Close();
                return true;
            }
            return false;
        }

        public IEnumerable<IDockContainer> GetDocks()
        {
            foreach (DockPage page in Items)
            {
                if (page.Widget is IDockContainer)
                    yield return (IDockContainer)page.Widget;
            }
        }

        #endregion

        protected override void OnContextMenuShow(ButtonEventArgs e)
        {
            if (widgetBounds.Contains(e.Position))
                return;
            base.OnContextMenuShow(e);
        }

    }
}
