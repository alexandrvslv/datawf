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
        private bool viewImage = true;
        private Rectangle widgetRect;

        public DockPanel() : base()
        {
            Items.StyleName = "Window";
            Indent = 1;

            //context = new Menubar(toolHide);
            //toolHide = new ToolMenuItem { Name = "Hide", Text = "Hide" };

            Name = "DockPanel";
        }

        public DockItem DockItem { get; set; }

        public DockBox DockBox { get { return DockItem?.DockBox; } }

        public Widget CurrentWidget
        {
            get { return widget; }
            set
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
                    SetChildBounds(widget, widgetRect);
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

        public DockPanel(params Widget[] widgets) : this()
        {
            foreach (var widget in widgets)
            {
                Put(widget);
            }
        }

        public void RemovePage(DockPage page)
        {
            if (page == null)
                return;
            if (CurrentWidget == page.Widget)
            {
                DockPage npage = null;
                if (pagesHistory.Last != null)
                {
                    var item = pagesHistory.Last;

                    while (item != null && (item.Value == page || !item.Value.Visible))
                        item = item.Previous;
                    if (item != null)
                        npage = item.Value;
                }
                CurrentPage = npage;
            }

            while (pagesHistory.Remove(page))
            {
            }
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            double def = 30;
            var pagesRect = Rectangle.Zero;
            widgetRect = Rectangle.Zero;
            if (pagesAlign == LayoutAlignType.Top)
            {
                if (ItemOrientation == Orientation.Vertical)
                    def = 100D;
                pagesRect = new Rectangle(0D, 0D, Size.Width, def);
                widgetRect = new Rectangle(0D, def, Size.Width, Size.Height - def);
            }
            else if (pagesAlign == LayoutAlignType.Bottom)
            {
                if (ItemOrientation == Orientation.Vertical)
                    def = 100D;

                pagesRect = new Rectangle(0D, Size.Height - def, Size.Width, def);
                widgetRect = new Rectangle(0D, 0D, Size.Width, Size.Height - def);
            }
            else if (pagesAlign == LayoutAlignType.Left)
            {
                if (ItemOrientation == Orientation.Horizontal)
                    def = 100;

                pagesRect = new Rectangle(0D, 0D, def, Size.Height);
                widgetRect = new Rectangle(def, 0D, Size.Width - def, Size.Height);
            }
            else if (pagesAlign == LayoutAlignType.Right)
            {
                if (ItemOrientation == Orientation.Horizontal)
                    def = 100;

                pagesRect = new Rectangle(Size.Width - def, 0D, def, Size.Height);
                widgetRect = new Rectangle(0D, 0D, Size.Width - def, Size.Height);
            }
            if (CurrentWidget != null)
            {
                SetChildBounds(CurrentWidget, widgetRect);
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
                if (value != null)
                {
                    if (DockItem != null && !DockItem.Visible)
                    {
                        DockItem.Visible = true;
                        //Parent.ResumeLayout(true);
                    }
                    pagesHistory.AddLast(value);
                    value.Checked = true;
                    CurrentWidget = value.Widget;
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

        //public void Serialize(ISerializeWriter writer)
        //{
        //    writer.WriteAttribute("Current", CurrentWidget?.Name ?? string.Empty);
        //    writer.Write(items);
        //    foreach (DockPage page in Items)
        //    {
        //        if (page.Widget is ISerializableElement)
        //        {
        //            writer.Write(page.Widget, page.Widget.Name, true);
        //        }
        //        else
        //        {

        //        }
        //    }
        //}

        //public void Deserialize(ISerializeReader reader)
        //{
        //    var current = reader.ReadAttribute<string>("Current");
        //    if (reader.IsEmpty)
        //        return;

        //    while (reader.ReadBegin())
        //    {
        //        var type = reader.ReadType();
        //        DockPage page = GetPage(reader.CurrentName);
        //        if (page == null)
        //        {
        //            var widget = (Widget)EmitInvoker.CreateObject(type);
        //            widget.Name = reader.CurrentName;
        //            page = Put(widget);
        //        }
        //        if (page.Widget.GetType() == type && page.Widget is ISerializableElement)
        //        {
        //            ((ISerializableElement)page.Widget).Deserialize(reader);
        //        }
        //    }
        //    if (!string.IsNullOrEmpty(current))
        //    {
        //        CurrentPage = GetPage(current);
        //    }
        //}

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
            RemovePage(arg.Page);
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
                HideOnClose = widget is IDockContent ? ((IDockContent)widget).HideOnClose : false
            };
            Put(page);
            return page;
        }

        public void Put(DockPage page)
        {
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

    }
}
