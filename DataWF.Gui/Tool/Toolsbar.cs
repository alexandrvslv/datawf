using System;
using System.Collections.Generic;
using System.Diagnostics;
using DataWF.Common;
using Xwt;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class Toolsbar : Canvas, ILocalizable
    {
        private ToolLayoutMap items;
        private ToolItem cacheHitItem;
        private Menubar currentMenu;

        public Toolsbar()
        {
            items = new ToolLayoutMap() { Bar = this };
        }

        public Toolsbar(params ToolItem[] items) : this()
        {
            Items.AddRange(items);
        }

        public ToolLayoutMap Items
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

        public ToolItem this[string name]
        {
            get { return (ToolItem)Items[name]; }
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            var size = base.OnGetPreferredSize(widthConstraint, heightConstraint);
            items.GetBound(widthConstraint.AvailableSize, heightConstraint.AvailableSize);
            foreach (ToolItem item in LayoutMapHelper.GetVisibleItems(items))
            {
                item.CheckSize(false);
                items.GetBound(item);
            }
            return items.Bound.Size;
        }

        public void Reallocate()
        {
            OnReallocate();
            QueueDraw();
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();

            items.GetBound(Size.Width, Size.Height);
            foreach (ToolItem item in LayoutMapHelper.GetVisibleItems(items))
            {
                items.GetBound(item);
                if (item.Content != null && item.Content.Parent == this)
                    SetChildBounds(item.Content, item.GetContentBound());
            }
            QueueDraw();
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);
            var context = GraphContext.Default;
            context.Context = ctx;
            foreach (ToolItem item in LayoutMapHelper.GetVisibleItems(items))
            {
                item.OnDraw(context);
            }
            //context.DrawRectangle(GuiEnvir.Styles["Column"], dirtyRect);
        }

        public void Add(ToolItem item)
        {
            Items.Add(item);
        }

        protected ToolItem HitTest(Point position)
        {
            foreach (ToolItem item in LayoutMapHelper.GetVisibleItems(items))
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
                }
                cacheHitItem = hitItem;
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
            var hitItem = HitTest(args.Position);
            if (hitItem != null)
            {
                hitItem.OnButtonPressed(args);
            }
        }

        protected override void OnButtonReleased(ButtonEventArgs args)
        {
            base.OnButtonReleased(args);
            var hitItem = HitTest(args.Position);
            if (hitItem != null)
            {
                hitItem.OnButtonReleased(args);
            }
        }

        public void Localize()
        {
            foreach (ToolItem item in LayoutMapHelper.GetItems(Items))
                item.Localize();
        }

        public event EventHandler<ToolItemEventArgs> ItemClick;

        public void OnItemClick(ToolItem toolItem)
        {
            ItemClick?.Invoke(this, new ToolItemEventArgs { Item = toolItem });
        }
    }
}
