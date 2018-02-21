using System;
using System.Collections.Generic;
using System.ComponentModel;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class ToolLayoutMap : LayoutMap, IToolItem
    {
        [NonSerialized]
        private Canvas bar;

        public ToolLayoutMap()
        {
            //Indent = 4;
        }

        public Canvas Bar
        {
            get { return bar; }
            set
            {
                if (bar != value)
                {
                    if (bar != null)
                    {
                        foreach (IToolItem item in Items)
                        {
                            item.Bar = null;
                        }
                    }
                    bar = value;
                    if (bar != null)
                    {
                        foreach (IToolItem item in Items)
                        {
                            item.Bar = value;
                        }
                    }
                }
            }
        }

        public Widget Content { get; set; }

        protected override void OnItemsListChanged(object sender, ListChangedEventArgs e)
        {
            base.OnItemsListChanged(sender, e);
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {
                var toolItem = items[e.NewIndex] as IToolItem;
                if (toolItem != null)
                {
                    toolItem.Bar = bar;
                }
            }
            else if (e.ListChangedType == ListChangedType.ItemDeleted && e.NewIndex >= 0)
            {
                var toolItem = items[e.NewIndex] as IToolItem;
                if (toolItem != null)
                {
                    toolItem.Bar = null;
                }
            }
            //else if (e.ListChangedType == ListChangedType.Reset)
            //{
            //    if (items.Count == 0)
            //        bar.Clear();
            //}
            else if (e.ListChangedType == ListChangedType.ItemChanged)
            {
                bar.QueueForReallocate();
                bar.QueueDraw();
            }
        }

        public void Add(ToolItem item)
        {
            base.Add(item);
        }

        public void AddRange(IEnumerable<ToolItem> toolItems)
        {
            foreach (var item in toolItems)
            {
                Add(item);
            }
        }
    }
}
