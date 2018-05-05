using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class DockPanel : Canvas, IEnumerable, IEnumerable<DockPage>, IDockContainer, ILocalizable
    {
        private DockItem mapItem;
        private DockPage currentPage;
        private Menubar context;
        private ToolMenuItem toolHide;
        private LayoutAlignType pagesAlign = LayoutAlignType.Top;
        private DockPageBox pages;
        private VBox panel;
        private LinkedList<DockPage> pagesHistory = new LinkedList<DockPage>();

        public event EventHandler<DockPageEventArgs> PageSelected;

        private Widget widget;

        public DockPanel() : base()
        {
            toolHide = new ToolMenuItem { Name = "Hide", Text = "Hide" };

            context = new Menubar(toolHide);

            pages = new DockPageBox { Name = "toolStrip" };
            pages.PageClick += PagesPageClick;
            pages.Items.ListChanged += PageListOnChange;

            panel = new VBox
            {
                Margin = new WidgetSpacing(6, 0, 6, 6)
            };

            Name = "DockPanel";
            AddChild(pages);
            AddChild(panel);
            //BackgroundColor = Colors.Gray;
        }

        public DockPanel(params Widget[] widgets) : this()
        {
            foreach (var widget in widgets)
            {
                Put(widget);
            }
        }

        public void Localize()
        {
            foreach (DockPage page in pages.Items)
            {
                var loc = page.Widget as ILocalizable;
                if (loc != null)
                    loc.Localize();
            }
        }

        public DockPage AddPage(Widget c)
        {
            var page = DockBox.CreatePage(c);
            Pages.Items.Add(page);
            return page;
        }

        private void PagesPageClick(object sender, DockPageEventArgs e)
        {
            SelectPage(e.Page);
        }

        private void PageListOnChange(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {
                if (MapItem != null && !MapItem.Visible)
                {
                    MapItem.Visible = true;
                    //Parent.ResumeLayout(true);
                }
                DockPage page = pages.Items[e.NewIndex];
                SelectPage(page);
            }
            else if (e.ListChangedType == ListChangedType.ItemChanged)
            {
                DockPage page = pages.Items[e.NewIndex];
                if (CurrentWidget == page.Widget && !page.Visible)
                {
                    RemovePage(page, false);
                }
                else if (CurrentWidget == null && page.Visible)
                {
                    SelectPage(page);
                }
            }
            else if (e.ListChangedType == ListChangedType.ItemDeleted)
            {
                if (e.NewIndex >= 0)
                {
                    DockPage page = pages.Items[e.NewIndex];
                    RemovePage(page, true);
                }
                else
                {
                    if (pages.Items.Count > 0)
                    {
                        if (CurrentWidget == null)
                            SelectPage(pages.Items[0]);
                    }
                    else
                    {
                        if (MapItem != null && !MapItem.FillWidth)
                        {
                            MapItem.Visible = false;
                            foreach (var mapItem in MapItem.Map)
                            {
                                if (mapItem.Count == 0)
                                {
                                    if (mapItem.Panel.Pages.Items.Count == 0)
                                        mapItem.Visible = false;
                                }
                            }
                            if (Parent is DockBox)
                            {
                                ((DockBox)Parent).QueueForReallocate();
                            }
                        }
                    }
                }
            }
        }

        public void RemovePage(DockPage page, bool RemoveHistory)
        {
            if (page != null)
            {
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
                    SelectPage(npage);
                }

                if (RemoveHistory)
                    while (pagesHistory.Remove(page))
                    {
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
                    pages.Orientation = Orientation.Vertical;
                else
                    pages.Orientation = Orientation.Horizontal;
                //PerformLayout();
            }
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            double def = 25;
            var pagesRect = Rectangle.Zero;
            var widgetRect = Rectangle.Zero;
            if (pagesAlign == LayoutAlignType.Top)
            {
                if (pages.ItemOrientation == Orientation.Vertical)
                    def = 100D;
                pagesRect = new Rectangle(0D, 0D, Size.Width, def);
                widgetRect = new Rectangle(0D, def, Size.Width, Size.Height - def);
            }
            else if (pagesAlign == LayoutAlignType.Bottom)
            {
                if (pages.ItemOrientation == Orientation.Vertical)
                    def = 100D;

                pagesRect = new Rectangle(0D, Size.Height - def, Size.Width, def);
                widgetRect = new Rectangle(0D, 0D, Size.Width, Size.Height - def);
            }
            else if (pagesAlign == LayoutAlignType.Left)
            {
                if (pages.ItemOrientation == Orientation.Horizontal)
                    def = 100;

                pagesRect = new Rectangle(0D, 0D, def, Size.Height);
                widgetRect = new Rectangle(def, 0D, Size.Width - def, Size.Height);
            }
            else if (pagesAlign == LayoutAlignType.Right)
            {
                if (pages.ItemOrientation == Orientation.Horizontal)
                    def = 100;

                pagesRect = new Rectangle(Size.Width - def, 0D, def, Size.Height);
                widgetRect = new Rectangle(0D, 0D, Size.Width - def, Size.Height);
            }
            SetChildBounds(pages, pagesRect);
            SetChildBounds(panel, widgetRect);
        }

        public DockPageBox Pages
        {
            get { return pages; }
        }

        public DockItem MapItem
        {
            get { return mapItem; }
            set { mapItem = value; }
        }

        public Widget CurrentWidget
        {
            get { return widget; }
            set
            {
                if (value == widget)
                    return;

                if (widget != null)
                {
                    panel.Remove(widget);
                    widget.Visible = false;
                }

                widget = value;

                if (widget != null)
                {
                    widget.Visible = true;
                    panel.PackStart(widget, true);
                }
            }
        }

        public int Count
        {
            get { return pages.Items.Count; }
        }

        public void ClearPages()
        {
            pages.Items.Clear();
        }

        public void SelectPageByControl(Widget control)
        {
            var page = GetPage(control);
            if (page != null)
            {
                SelectPage(page);
            }
        }

        public DockPage CurrentPage
        {
            get { return currentPage; }
        }

        public void SelectPage(DockPage page)
        {
            if (currentPage == page)
                return;

            currentPage = page;
            if (page != null)
            {
                pagesHistory.AddLast(page);
                page.Active = true;
                CurrentWidget = page.Widget;
            }
            else
            {
                CurrentWidget = null;
            }
            if (PageSelected != null)
                PageSelected(this, new DockPageEventArgs(page));
        }

        #region IDockContainer implementation

        public IDockContainer DockParent
        {
            get { return GuiService.GetDockParent(this); }
        }

        public bool Contains(Widget control)
        {
            foreach (DockPage t in pages.Items)
                if (t.Widget == control)
                    return true;
            return false;
        }

        public IEnumerable<Widget> GetControls()
        {
            foreach (DockPage t in pages.Items)
                yield return t.Widget;
        }

        public Widget Find(string name)
        {
            foreach (DockPage page in pages.Items)
                if (page.Widget.Name == name)
                    return page.Widget;
            return null;
        }

        public DockPage Put(Widget control)
        {
            return Put(control, DockType.Content);
        }

        public DockPage Put(Widget control, DockType type)
        {
            var page = DockBox.CreatePage(control);
            Put(page);
            return page;
        }

        public void Put(DockPage page)
        {
            pages.Items.Add(page);
        }

        public DockPage GetPage(string name)
        {
            foreach (DockPage page in pages.Items)
                if (page.Widget?.Name == name)
                {
                    return page;
                }
            return null;
        }

        public DockPage GetPage(Widget control)
        {
            foreach (DockPage page in pages.Items)
                if (page.Widget == control)
                {
                    return page;
                }
            return null;
        }

        public bool Delete(Widget control)
        {
            var tp = GetPage(control);
            if (tp != null)
            {
                tp.List.Remove(tp);
                return true;
            }
            return false;
        }

        public IEnumerable<IDockContainer> GetDocks()
        {
            foreach (DockPage page in pages.Items)
            {
                if (page.Widget is IDockContainer)
                    yield return (IDockContainer)page.Widget;
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            pages.Dispose();
            base.Dispose(disposing);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return pages.Items.GetEnumerator();
        }

        public IEnumerator<DockPage> GetEnumerator()
        {
            return pages.Items.GetEnumerator();
        }
    }
}
