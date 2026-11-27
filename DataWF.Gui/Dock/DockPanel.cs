using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DataWF;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using System.Text.RegularExpressions;
using System.Globalization;

namespace DataWF.Gui
{
    public class DockPanel : Toolsbar, IEnumerable, IEnumerable<DockPage>, IDockContainer, ILocalizable, IXMLSerializable
    {
        private DockPage currentPage;
        //private Menubar context;
        //private ToolMenuItem toolHide;
        private LayoutAlignType pagesAlign = LayoutAlignType.Top;
        private LinkedList<DockPage> pagesHistory = new LinkedList<DockPage>();

        private IControl control;
        private BoxOrientation orientation = BoxOrientation.Horizontal;
        private bool viewClose = true;
        private Rect ControlBounds;
        private FormattedText selectText;

        public DockPanel() : base()
        {
            Items.StyleName = "Window";
            Indent = 1;
            //context = new Menubar(toolHide);
            //toolHide = new ToolMenuItem { Name = "Hide", Text = "Hide" };

            Name = nameof(DockPanel);
        }

        public DockPanel(params Control[] Controls) : this()
        {
            foreach (var Control in Controls)
            {
                Put(Control);
            }
        }

        public DockItem DockItem { get; set; }

        public DockBox DockBox { get { return DockItem?.DockBox; } }

        public IControl CurrentControl
        {
            get => control;
            internal set
            {
                if (value == control)
                    return;

                if (control != null)
                {
                    Children.Remove(control);
                }

                control = value;

                if (control != null)
                {
                    Children.Add(control);
                    control.Arrange(ControlBounds);
                }
            }
        }

        public LayoutAlignType PagesAlign
        {
            get => pagesAlign;
            set
            {
                if (pagesAlign == value)
                    return;
                pagesAlign = value;
                if (pagesAlign == LayoutAlignType.Left ||
                    pagesAlign == LayoutAlignType.Right)
                    Orientation = BoxOrientation.Vertical;
                else
                    Orientation = BoxOrientation.Horizontal;
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
            InvalidateVisual();
        }

        protected override void OnMeasureInvalidated()
        {
            base.OnMeasureInvalidated();
            double def = 30;
            var pagesRect = Rect.Empty;
            ControlBounds = Rect.Empty;
            if (pagesAlign == LayoutAlignType.Top)
            {
                if (ItemOrientation == BoxOrientation.Vertical)
                    def = 100D;
                pagesRect = new Rect(0D, 0D, Width, def);
                ControlBounds = new Rect(0D, def, Width, Height - def);
            }
            else if (pagesAlign == LayoutAlignType.Bottom)
            {
                if (ItemOrientation == BoxOrientation.Vertical)
                    def = 100D;

                pagesRect = new Rect(0D, Height - def, Width, def);
                ControlBounds = new Rect(0D, 0D, Width, Height - def);
            }
            else if (pagesAlign == LayoutAlignType.Left)
            {
                if (ItemOrientation == BoxOrientation.Horizontal)
                    def = 100;

                pagesRect = new Rect(0D, 0D, def, Height);
                ControlBounds = new Rect(def, 0D, Width - def, Height);
            }
            else if (pagesAlign == LayoutAlignType.Right)
            {
                if (ItemOrientation == BoxOrientation.Horizontal)
                    def = 100;

                pagesRect = new Rect(Width - def, 0D, def, Height);
                ControlBounds = new Rect(0D, 0D, Width - def, Height);
            }
            if (CurrentControl != null)
            {
                CurrentControl.Arrange(ControlBounds);
            }
        }

        public void ClearPages()
        {
            Items.Clear();
            CurrentPage = null;
        }

        public void SelectPageByControl(Control control)
        {
            var page = GetPage(control);
            if (page != null)
            {
                CurrentPage = page;
            }
        }

        public DockPage CurrentPage
        {
            get => currentPage;
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
                    CurrentControl = currentPage.Control;
                }
                else
                {
                    CurrentControl = null;
                }
                DockBox?.OnPageSelected(this, new DockPageEventArgs(value));
            }
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
            get => viewClose;
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

        public BoxOrientation Orientation
        {
            get => orientation;
            set
            {
                if (orientation == value)
                    return;
                orientation = value;
                InvalidateArrange();
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
            if (page.Control is IDockContent)
            {
                if (!((IDockContent)page.Control).Closing())
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
                page.Control = null;
            }
        }

        protected override void Render(GraphContext context)
        {
            base.Render(context);
            Rect brect = orientation == BoxOrientation.Horizontal
                ? new Rect(0, 30 - 5, Bounds.Width, 5)
                : new Rect(Bounds.Width - 5, 0, 5, 30);
            if (PagesAlign == LayoutAlignType.Bottom)
                brect = brect.WithY(0);
            context.FillRectangle(items.Style, brect, CellDisplayState.Selected);//st.BackBrush.GetBrush(rectb, 

            if (CurrentControl == null && ControlBounds.Width > 0 && ControlBounds.Height > 0)
            {
                selectText ??= new FormattedText
                (
                    textToFormat: "Select Document",
                    culture: CultureInfo.InvariantCulture,
                    flowDirection: FlowDirection.LeftToRight,
                    typeface: Typeface.Default,
                    emSize: 5D

                );
                var bound = new Rect(ControlBounds.X, ControlBounds.Height / 2D, ControlBounds.Width, 100);
                context.DrawText(selectText, bound, items.Style.FontBrush.Color.WithAlpha(0.3), TextAlignment.Center);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var baseSize = base.ArrangeOverride(finalSize);
            if (CurrentControl != null)
            {
                var size = CurrentControl.PreviousMeasure.Value;
                size = new Size(size.Width, size.Height + baseSize.Height);
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

        public bool Contains(IControl control)
        {
            foreach (DockPage t in Items)
                if (t.Control == control)
                    return true;
            return false;
        }

        public IEnumerable<IControl> GetControls()
        {
            foreach (DockPage t in Items)
                yield return t.Control;
        }

        public IControl Find(string name)
        {
            foreach (DockPage page in Items)
                if (page.Control.Name == name)
                    return page.Control;
            return null;
        }

        public DockPage Put(IControl Control)
        {
            return Put(Control, DockType.Content);
        }

        public DockPage Put(IControl Control, DockType type)
        {
            var page = new DockPage
            {
                Name = Control.Name,
                Control = Control,
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
                if (page.Control?.Name == name)
                {
                    return page;
                }
            return null;
        }

        public DockPage GetPage(IControl control)
        {
            foreach (DockPage page in Items)
                if (page.Control == control)
                {
                    return page;
                }
            return null;
        }

        public bool Delete(IControl control)
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
                if (page.Control is IDockContainer)
                    yield return (IDockContainer)page.Control;
            }
        }

        #endregion

        protected override void OnContextMenuShow(PointerEventArgs e, Point position)
        {
            if (ControlBounds.Contains(position))
                return;
            base.OnContextMenuShow(e, position);
        }

    }
}
