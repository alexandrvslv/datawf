using System;
using System.ComponentModel;
using Xwt;
using System.Linq;
using Xwt.Drawing;

namespace DataWF.Gui
{
    public class GroupBox : Canvas
    {
        private GroupBoxMap map;
        private bool flag = false;

        public GroupBox()
        {
            map = new GroupBoxMap(this);
            map.CalcHeight = CalcHeight;
        }

        public void ResizeLayout()
        {
            flag = true;
            foreach (ILayoutItem item in map.Items)
            {
                if (item is GroupBoxMap)
                {
                    ((GroupBoxMap)item).GroupBox.flag = true;
                }
            }
            map.Width = Size.Width;
            map.Height = Size.Height;
            map.GetBound();

            foreach (GroupBoxItem item in map.GetItems())
            {
                item.CheckBounds();
            }

            foreach (ILayoutItem item in map.Items)
            {
                if (item is GroupBoxMap)
                    ((GroupBoxMap)item).GroupBox.flag = false;
            }
            flag = false;
            QueueDraw();
        }

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
                    height = box.HeaderHeight + 10;
                }
                if (height > map.Bound.Height)
                    height = map.Bound.Height;
                return height;
            }
            return item.Height;
        }

        public void Add(ILayoutItem item)
        {
            if (item.Map == null)
                map.Add(item);
            if (item is GroupBoxItem)
            {
                var box = (GroupBoxItem)item;
                if (box.Widget != null && box.Widget.Parent != this)
                {
                    if (!Children.Contains(box.Widget))
                        base.AddChild(box.Widget);
                }
            }
            else if (item is GroupBoxMap)
            {
                var box = ((GroupBoxMap)item);
                if (box.GroupBox != null && box.GroupBox != this)
                {
                    if (!Children.Contains(box.GroupBox))
                        base.AddChild(box.GroupBox);
                }
                else
                {
                    box.CalcHeight = CalcHeight;
                    foreach (var sitem in box.Items)
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
            map.GetBound(widthConstraint.AvailableSize, heightConstraint.AvailableSize);
            return map.Bound.Size;
        }

        protected override void OnButtonReleased(ButtonEventArgs args)
        {
            base.OnButtonReleased(args);
            foreach (GroupBoxItem box in map.GetItems())
            {
                if (box.Visible)
                {
                    if (box.Bound.Contains(args.X, args.Y))
                    {
                        if (box.GetExpandBound(box.Bound).Contains(args.X, args.Y))
                        {
                            box.Expand = !box.Expand;
                            QueueForReallocate();
                        }
                    }
                }
            }
        }

        protected override void OnDraw(Context ctx, Rectangle dirtyRect)
        {
            base.OnDraw(ctx, dirtyRect);
            GraphContext.Default.Context = ctx;

            foreach (ILayoutItem item in map.GetItems())
                if (item.Visible && item is GroupBoxItem)
                    ((GroupBoxItem)item).Paint(GraphContext.Default);
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
            if (!flag)
                ResizeLayout();
        }

        public bool Contains(ILayoutItem item)
        {
            return map.Contains(item);
        }

        public void Sort()
        {
            map.Items.Sort(new Comparison<ILayoutItem>(LayoutMapHelper.Compare));
        }

        public int Row
        {
            get { return map.Row; }
            set { map.Row = value; }
        }

        public int Col
        {
            get { return map.Col; }
            set { map.Col = value; }
        }

        [DefaultValue(false)]
        public bool FillWidth
        {
            get { return map.FillWidth; }
            set { map.FillWidth = value; }
        }

        [DefaultValue(false)]
        public bool FillHeight
        {
            get { return map.FillHeight; }
            set { map.FillHeight = value; }
        }

        public GroupBoxMap Map { get { return map; } }
    }
}

