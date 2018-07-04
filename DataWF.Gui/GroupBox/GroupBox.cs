using System;
using System.ComponentModel;
using Xwt;
using System.Linq;
using Xwt.Drawing;
using DataWF.Common;

namespace DataWF.Gui
{
    public class GroupBox : Canvas, ILocalizable
    {
        private GroupBoxItem items;

        public GroupBox()
        {
            items = new GroupBoxItem(this) { CalcHeight = CalcHeight };
        }

        public GroupBox(params GroupBoxItem[] items) : this()
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public int Row
        {
            get { return items.Row; }
            set { items.Row = value; }
        }

        public int Col
        {
            get { return items.Col; }
            set { items.Col = value; }
        }

        [DefaultValue(false)]
        public bool FillWidth
        {
            get { return items.FillWidth; }
            set { items.FillWidth = value; }
        }

        [DefaultValue(false)]
        public bool FillHeight
        {
            get { return items.FillHeight; }
            set { items.FillHeight = value; }
        }

        public GroupBoxItem Items { get { return items; } }

        protected double CalcHeight(ILayoutItem item)
        {
            if (item is GroupBoxItem)
            {
                var box = (GroupBoxItem)item;

                double height = 0;
                if (box.Expand)
                {
                    if (box.Widget != null && box.Autosize)
                    {
                        var size = box.Widget.Surface.GetPreferredSize(SizeConstraint.Unconstrained, SizeConstraint.Unconstrained);
                        //if (box.Autosize);// || size.Height > box.DefaultHeight)
                        return size.Height + box.HeaderHeight + 10;
                    }
                    height = box.Height + box.HeaderHeight + 10;
                }
                else
                {
                    height = box.HeaderHeight;
                }
                //if (height > map.Bound.Height)
                //    height = map.Bound.Height;
                return height;
            }
            return item.Height;
        }

        public void Add(GroupBoxItem item)
        {
            if (item.Map == null)
                items.Add(item);
            if (item.Count == 0)
            {
                if (item.Widget != null && item.Widget.Parent != this)
                {
                    if (!Children.Contains(item.Widget))
                        base.AddChild(item.Widget);
                }
            }
            else
            {
                item.GroupBox = this;
                if (item.GroupBox != null && item.GroupBox != this)
                {
                    if (!Children.Contains(item.GroupBox))
                        base.AddChild(item.GroupBox);
                }
                else
                {
                    item.CalcHeight = CalcHeight;
                    foreach (var sitem in item)
                    {
                        Add(sitem);
                    }
                }
            }
        }

        protected override Size OnGetPreferredSize(SizeConstraint widthConstraint, SizeConstraint heightConstraint)
        {
            base.OnGetPreferredSize(widthConstraint, heightConstraint);
            //foreach (ILayoutItem item in map.Items)
            //{
            //    if (item is GroupBoxItem && ((GroupBoxItem)item).Widget != null)
            //    {
            //        item.Height = CalcHeight(item);
            //    }
            //    else if (item is GroupBoxMap)
            //    {
            //        var box = (GroupBoxMap)item;
            //        box.GroupBox.OnGetPreferredSize(widthConstraint, heightConstraint);
            //    }
            //}
            var bound = items.GetBound(widthConstraint.AvailableSize, heightConstraint.AvailableSize);
            foreach (var item in items.GetVisibleItems())
            {
                item.Widget.Surface.GetPreferredSize();
            }
            return bound.Size;
        }

        protected override void OnButtonReleased(ButtonEventArgs args)
        {
            base.OnButtonReleased(args);
            foreach (var box in items.GetVisibleItems())
            {
                if (box.Visible)
                {
                    if (box.Bound.Contains(args.X, args.Y))
                    {
                        if (box.GetExpandBound(box.Bound).Contains(args.X, args.Y))
                        {
                            box.Expand = !box.Expand;
                        }
                    }
                }
            }
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);
            using (var context = new GraphContext(ctx))
            {
                foreach (ILayoutItem item in items.GetVisibleItems())
                {
                    ((GroupBoxItem)item).Paint(context);
                }
            }
        }

        public new void AddChild(Widget widget)
        {
            base.AddChild(widget);
            if (!(widget is GroupBox))
            {
                Add(new GroupBoxItem() { Widget = widget });
            }
        }

        protected override void OnReallocate()
        {
            base.OnReallocate();
            items.Width = Size.Width;
            items.Height = Size.Height;
            var mapBound = items.GetBound();

            foreach (GroupBoxItem item in items.GetVisibleItems())
            {
                items.GetBound(item, mapBound);
            }
            QueueDraw();
        }

        public bool Contains(ILayoutItem item)
        {
            return items.Contains(item);
        }

        public void Sort()
        {
            items.Sort(new Comparison<ILayoutItem>(GroupBoxItem.Compare));
        }

        public void Localize()
        {
            foreach (GroupBoxItem item in items.GetItems())
            {
                item.Localize();
            }
        }

        protected override void Dispose(bool disposing)
        {
            items.Dispose();

            base.Dispose(disposing);
        }
    }
}

