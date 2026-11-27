using System;
using System.Collections.Generic;
using System.Diagnostics;
using DataWF;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;

namespace DataWF.Gui
{
    public class Toolsbar : Canvas, ILocalizable
    {
        public static ToolsbarMenu DefaultMenu { get; private set; }

        protected ToolItem items;
        protected ToolItem cacheHitItem;
        protected ToolItem hitItem;
        protected Menubar currentMenu;
        protected BoxOrientation itemOrientation = BoxOrientation.Horizontal;

        public Toolsbar()
        {
            items = new ToolItem() { Bar = this };
            items.StyleName = "Toolsbar";
            Name = "Bar";
        }

        public Toolsbar(params ToolItem[] items) : this()
        {
            Items.AddRange(items);
        }

        public double MinItemHeight { get; set; } = 28;

        public double MinItemWidth { get; set; } = 28;

        public double Indent { get; set; } = 5D;

        public ToolItem Items
        {
            get => items;
            set
            {
                if (items != value)
                {
                    items = value;
                    if (items != null)
                        items.Bar = this;
                }
            }
        }

        public ToolItem Owner { get; set; }

        public Menubar CurrentMenubar
        {
            get => currentMenu;
            internal set
            {

                if (currentMenu != null)
                {
                    ((ToolDropDown)currentMenu.OwnerItem).HideMenu();
                }

                if (currentMenu == value)
                {
                    currentMenu = null;
                    return;
                }

                currentMenu = value;

                if (currentMenu != null)
                {
                    ((ToolDropDown)currentMenu.OwnerItem).ShowMenu();
                }
            }
        }

        public BoxOrientation ItemOrientation
        {
            get => itemOrientation;
            set
            {
                if (itemOrientation == value)
                    return;
                itemOrientation = value;
                items.GrowMode = value;
            }
        }


        protected internal virtual void OnItemMove(ToolItemEventArgs args)
        {

        }

        public ToolItem this[string name]
        {
            get { return (ToolItem)Items[name]; }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = base.MeasureOverride(availableSize);
            if (!items.Disposed)
            {
                var bound = items.GetBound(0, 0);
                foreach (var item in items.GetVisibleItems().OfType<ToolContentItem>())
                {
                    item.Content?.Measure(item.Bound.Size);
                }
                return bound.Size;
            }
            return size;
        }

        public void Reallocate()
        {
            InvalidateMeasure();
            InvalidateVisual();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            items.Width = Width;
            items.Height = Height;
            var mapBound = items.GetBound();
            foreach (ToolItem item in items.GetVisibleItems())
            {
                item.CheckSize();
                items.GetBound(item, mapBound);
                if (item is ToolContentItem toolContentItem
                    && toolContentItem.Content != null
                    && toolContentItem.Content.Parent == this)
                    toolContentItem.Content.Arrange(toolContentItem.ContentBound);
            }
            InvalidateVisual();
            return finalSize;

        }

        public override void Render(DrawingContext ctx)
        {
            base.Render(ctx);
            using (var context = new GraphContext(ctx))
            {
                Render(context);
            }
        }

        protected virtual void Render(GraphContext context)
        {
            if (items.Style != null)
            {
                context.DrawCell(items.Style, null, Bounds, Bounds, CellDisplayState.Default);
            }
            foreach (ToolItem item in items.GetVisibleItems())
            {
                item.Render(context);
            }
        }

        public void Add(ToolItem item)
        {
            Items.Add(item);
        }

        protected ToolItem HitTest(Point position)
        {
            foreach (ToolItem item in items.GetVisibleItems())
            {
                if (item.Bound.Contains(position))
                {
                    return item;
                }
            }
            return null;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            var hitItem = HitTest(e.GetPosition(this));
            if (cacheHitItem != hitItem)
            {
                if (cacheHitItem != null)
                {
                    cacheHitItem.OnPointerExited(e);
                }
                if (hitItem != null)
                {
                    hitItem.OnMouseEntered(EventArgs.Empty);
                    if (!hitItem.DisplayStyle.HasFlag(ToolItemDisplayStyle.Text))
                    {
                        ToolTip.SetTip(this, hitItem.Text);
                        //Debug.WriteLine($"Tooltip Text {TooltipText}");
                    }
                    else
                    {
                        ToolTip.SetTip(this, null);
                    }
                }
                cacheHitItem = hitItem;
            }
            else if (cacheHitItem != null)
            {
                cacheHitItem.OnMouseMove(e);
            }
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            if (cacheHitItem != null)
            {
                cacheHitItem.OnPointerExited(e);
                cacheHitItem = null;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            var position = e.GetPosition(this);
            hitItem = HitTest(position);
            if (hitItem != null)
            {
                hitItem.OnPointerPressed(e);
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            var position = e.GetPosition(this);
            var info = e.GetCurrentPoint(this).Properties;
            hitItem = HitTest(position);
            if (hitItem != null && info.IsLeftButtonPressed)
            {
                hitItem.OnPointerReleased(e);
            }
            if (info.IsRightButtonPressed)
            {
                OnContextMenuShow(e, position);
            }
        }

        protected virtual void OnContextMenuShow(PointerEventArgs e, Point position)
        {
            DefaultMenu ??= new ToolsbarMenu();
            DefaultMenu.ContextBar = this;
            DefaultMenu.Show(this, PixelPoint.FromPoint(position, 1));
        }

        public void Localize()
        {
            foreach (ToolItem item in Items.GetItems())
                item.Localize();
        }

        public event EventHandler<ToolItemEventArgs> ItemClick;

        public virtual void OnItemClick(ToolItem item)
        {
            ItemClick?.Invoke(this, new ToolItemEventArgs { Item = item });
        }

        public virtual void OnItemDoubleClick(ToolItem item)
        { }

        public void Serialize(XmlInvokerWriter writer)
        {
            writer.Write(items);
        }

        public void Deserialize(XmlInvokerReader reader)
        {
            reader.Read(items);
        }
    }
}
