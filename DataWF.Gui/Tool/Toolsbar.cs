using System;
using System.Collections.Generic;
using System.Diagnostics;
using DataWF.Common;
using System.Linq;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class Toolsbar : Canvas, ILocalizable
    {
        public static ToolsbarMenu DefaultMenu { get; private set; }

        protected ToolItem items;
        protected ToolItem cacheHitItem;
        protected ToolItem hitItem;
        protected Menubar currentMenu;
        protected Orientation itemOrientation = Orientation.Horizontal;

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
            get { return items; }
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
            get { return currentMenu; }
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

        public Orientation ItemOrientation
        {
            get { return itemOrientation; }
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

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            var size = base.OnGetPreferredSize(widthConstraint, heightConstraint);
            var bound = items.GetBound(0, 0);
            foreach (var item in items.GetVisibleItems().OfType<ToolContentItem>())
            {
                item.Content?.Surface.GetPreferredSize();
            }
            return bound.Size;
        }

        public void Reallocate()
        {
            OnReallocate();
            QueueDraw();
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            items.Width = Size.Width;
            items.Height = Size.Height;
            var mapBound = items.GetBound();
            foreach (ToolItem item in items.GetVisibleItems())
            {
                item.CheckSize();
                items.GetBound(item, mapBound);
                if (item is ToolContentItem && ((ToolContentItem)item).Content != null && ((ToolContentItem)item).Content.Parent == this)
                    SetChildBounds(((ToolContentItem)item).Content, ((ToolContentItem)item).ContentBound);
            }
            QueueDraw();
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);
            using (var context = new GraphContext(ctx))
            {
                OnDraw(context);
            }
        }

        protected virtual void OnDraw(GraphContext context)
        {
            if (items.Style != null)
            {
                context.DrawCell(items.Style, null, Bounds, Bounds, CellDisplayState.Default);
            }
            foreach (ToolItem item in items.GetVisibleItems())
            {
                item.OnDraw(context);
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

        protected override void OnMouseMoved(MouseMovedEventArgs args)
        {
            base.OnMouseMoved(args);
            var hitItem = HitTest(args.Position);
            if (cacheHitItem != hitItem)
            {
                if (cacheHitItem != null)
                {
                    cacheHitItem.OnMouseExited(EventArgs.Empty);
                }
                if (hitItem != null)
                {
                    hitItem.OnMouseEntered(EventArgs.Empty);
                    if (!hitItem.DisplayStyle.HasFlag(ToolItemDisplayStyle.Text))
                    {
                        TooltipText = hitItem.Text;
                        //Debug.WriteLine($"Tooltip Text {TooltipText}");
                    }
                    else
                    {
                        TooltipText = null;
                    }
                }
                cacheHitItem = hitItem;
            }
            else if (cacheHitItem != null)
            {
                cacheHitItem.OnMouseMove(args);
            }
        }

        protected override void OnMouseExited(EventArgs args)
        {
            base.OnMouseExited(args);
            if (cacheHitItem != null)
            {
                cacheHitItem.OnMouseExited(args);
                cacheHitItem = null;
            }
        }

        protected override void OnButtonPressed(ButtonEventArgs args)
        {
            base.OnButtonPressed(args);
            hitItem = HitTest(args.Position);
            if (hitItem != null)
            {
                hitItem.OnButtonPressed(args);
            }
        }

        protected override void OnButtonReleased(ButtonEventArgs args)
        {
            base.OnButtonReleased(args);
            hitItem = HitTest(args.Position);
            if (hitItem != null && args.Button == PointerButton.Left)
            {
                hitItem.OnButtonReleased(args);
            }
            if (args.Button == PointerButton.Right)
            {
                OnContextMenuShow(args);
            }
        }

        protected override void Dispose(bool disposing)
        {
            Items.Dispose();
            base.Dispose(disposing);
        }

        protected virtual void OnContextMenuShow(ButtonEventArgs e)
        {
            if (DefaultMenu == null)
                DefaultMenu = new ToolsbarMenu();
            DefaultMenu.ContextBar = this;
            DefaultMenu.Show(this, e.Position);
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
