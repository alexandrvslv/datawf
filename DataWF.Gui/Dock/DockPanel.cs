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
    public class DockPanel : Canvas, IEnumerable, IEnumerable<DockPage>, IDockContainer, ILocalizable, ISerializableElement
    {
        private DockPage currentPage;
        private Menubar context;
        private ToolMenuItem toolHide;
        private LayoutAlignType pagesAlign = LayoutAlignType.Top;
        private VBox panel;
        private LinkedList<DockPage> pagesHistory = new LinkedList<DockPage>();

        private Widget widget;

        public DockPanel() : base()
        {
            //context = new Menubar(toolHide);
            toolHide = new ToolMenuItem { Name = "Hide", Text = "Hide" };
            Pages = new DockPageBox { Name = "pages" };
            panel = new VBox { Margin = new WidgetSpacing(6, 0, 6, 6) };

            Name = "DockPanel";
            AddChild(Pages);
            AddChild(panel);
        }

        public DockPageBox Pages { get; internal set; }

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
                    panel.Remove(widget);
                }

                widget = value;

                if (widget != null)
                {
                    panel.PackStart(widget, true);
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
                    Pages.Orientation = Orientation.Vertical;
                else
                    Pages.Orientation = Orientation.Horizontal;
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

        public void Localize()
        {
            Pages.Localize();
        }

        public DockPage AddPage(Widget c)
        {
            var page = DockBox.CreatePage(c);
            Pages.Items.Add(page);
            return page;
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
                CurrentPage = page;
            }

            while (pagesHistory.Remove(page))
            {
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
                if (Pages.ItemOrientation == Orientation.Vertical)
                    def = 100D;
                pagesRect = new Rectangle(0D, 0D, Size.Width, def);
                widgetRect = new Rectangle(0D, def, Size.Width, Size.Height - def);
            }
            else if (pagesAlign == LayoutAlignType.Bottom)
            {
                if (Pages.ItemOrientation == Orientation.Vertical)
                    def = 100D;

                pagesRect = new Rectangle(0D, Size.Height - def, Size.Width, def);
                widgetRect = new Rectangle(0D, 0D, Size.Width, Size.Height - def);
            }
            else if (pagesAlign == LayoutAlignType.Left)
            {
                if (Pages.ItemOrientation == Orientation.Horizontal)
                    def = 100;

                pagesRect = new Rectangle(0D, 0D, def, Size.Height);
                widgetRect = new Rectangle(def, 0D, Size.Width - def, Size.Height);
            }
            else if (pagesAlign == LayoutAlignType.Right)
            {
                if (Pages.ItemOrientation == Orientation.Horizontal)
                    def = 100;

                pagesRect = new Rectangle(Size.Width - def, 0D, def, Size.Height);
                widgetRect = new Rectangle(0D, 0D, Size.Width - def, Size.Height);
            }
            SetChildBounds(Pages, pagesRect);
            SetChildBounds(panel, widgetRect);
        }

        public void ClearPages()
        {
            Pages.Items.Clear();
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
                    value.Active = true;
                    CurrentWidget = value.Widget;
                }
                else
                {
                    CurrentWidget = null;
                }
                DockBox?.OnPageSelected(this, new DockPageEventArgs(value));
            }
        }

        #region IDockContainer implementation

        public IDockContainer DockParent
        {
            get { return GuiService.GetDockParent(this); }
        }

        public bool Contains(Widget control)
        {
            foreach (DockPage t in Pages.Items)
                if (t.Widget == control)
                    return true;
            return false;
        }

        public IEnumerable<Widget> GetControls()
        {
            foreach (DockPage t in Pages.Items)
                yield return t.Widget;
        }

        public Widget Find(string name)
        {
            foreach (DockPage page in Pages.Items)
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
            Pages.Items.Add(page);
            CurrentPage = page;
        }

        public DockPage GetPage(string name)
        {
            foreach (DockPage page in Pages.Items)
                if (page.Widget?.Name == name)
                {
                    return page;
                }
            return null;
        }

        public DockPage GetPage(Widget control)
        {
            foreach (DockPage page in Pages.Items)
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
            foreach (DockPage page in Pages.Items)
            {
                if (page.Widget is IDockContainer)
                    yield return (IDockContainer)page.Widget;
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            Pages.Dispose();
            base.Dispose(disposing);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Pages.Items.GetItems().GetEnumerator();
        }

        public IEnumerator<DockPage> GetEnumerator()
        {
            return Pages.Items.GetItems().Cast<DockPage>().GetEnumerator();
        }

        public void Serialize(ISerializeWriter writer)
        {
            writer.WriteAttribute("Current", CurrentWidget?.Name ?? string.Empty);
            foreach (DockPage page in Pages.Items)
            {
                if (page.Widget is ISerializableElement)
                {
                    writer.Write(page.Widget, page.Widget.Name, true);
                }
                else
                {

                }
            }
        }

        public void Deserialize(ISerializeReader reader)
        {
            var current = reader.ReadAttribute<string>("Current");
            if (reader.IsEmpty)
                return;

            while (reader.ReadBegin())
            {
                var type = reader.ReadType();
                DockPage page = GetPage(reader.CurrentName);
                if (page == null)
                {
                    var widget = (Widget)EmitInvoker.CreateObject(type);
                    widget.Name = reader.CurrentName;
                    page = Put(widget);
                }
                if (page.Widget.GetType() == type && page.Widget is ISerializableElement)
                {
                    ((ISerializableElement)page.Widget).Deserialize(reader);
                }
            }
        }
    }
}
