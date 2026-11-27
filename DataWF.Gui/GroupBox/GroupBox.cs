using DataWF;
using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

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
            get => items.Row;
            set => items.Row = value;
        }

        public int Col
        {
            get => items.Column;
            set => items.Column = value;
        }

        [DefaultValue(false)]
        public bool FillWidth
        {
            get => items.FillWidth;
            set => items.FillWidth = value;
        }

        [DefaultValue(false)]
        public bool FillHeight
        {
            get => items.FillHeight;
            set => items.FillHeight = value;
        }

        public GroupBoxItem Items { get { return items; } }

        protected double CalcHeight(ILayoutItem item)
        {
            if (item is GroupBoxItem box)
            {
                double height = 0;
                if (box.Expand)
                {
                    if (box.Control != null && box.Autosize)
                    {
                        var size = box.Control.Measure(box.Bound.Size);
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
                if (item.Control != null && item.Control.Parent != this)
                {
                    if (!Children.Contains(item.Control))
                        base.Children.Add(item.Control);
                }
            }
            else
            {
                item.GroupBox = this;
                if (item.GroupBox != null && item.GroupBox != this)
                {
                    if (!Children.Contains(item.GroupBox))
                        base.Children.Add(item.GroupBox);
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

        protected override Size MeasureOverride(Size aSize)
        {
            base.MeasureOverride(aSize);
            //foreach (ILayoutItem item in map.Items)
            //{
            //    if (item is GroupBoxItem && ((GroupBoxItem)item).Control != null)
            //    {
            //        item.Height = CalcHeight(item);
            //    }
            //    else if (item is GroupBoxMap)
            //    {
            //        var box = (GroupBoxMap)item;
            //        box.GroupBox.MeasureOverride(widthConstraint, heightConstraint);
            //    }
            //}
            var bound = items.GetBound(aSize.Width, aSize.Height);
            foreach (var item in items.GetVisibleItems())
            {
                item.Control.Measure(aSize);
            }
            return bound.Size;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs args)
        {
            base.OnPointerReleased(args);
            var position = args.GetPosition(this);
            foreach (var box in items.GetVisibleItems())
            {
                if (box.Visible)
                {
                    if (box.Bound.Contains(position))
                    {
                        if (box.GetExpandBound(box.Bound).Contains(position))
                        {
                            box.Expand = !box.Expand;
                        }
                    }
                }
            }
        }

        public override void Render(DrawingContext ctx)
        {
            base.Render(ctx);
            var dirtyRect = Bounds;
            using (var context = new GraphContext(ctx))
            {
                foreach (ILayoutItem item in items.GetVisibleItems())
                {
                    ((GroupBoxItem)item).Paint(context);
                }
            }
        }

        public void Add(IControl Control)
        {
            base.Children.Add(Control);
            if (!(Control is GroupBox))
            {
                Add(new GroupBoxItem() { Control = Control });
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            items.Width = Width;
            items.Height = Height;
            var mapBound = items.GetBound();

            foreach (GroupBoxItem item in items.GetVisibleItems())
            {
                items.GetBound(item, mapBound);
            }
            InvalidateVisual();
            return base.ArrangeOverride(finalSize);
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
    }
}

