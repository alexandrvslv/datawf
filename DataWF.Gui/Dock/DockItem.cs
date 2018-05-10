using System;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class DockItem : LayoutItem<DockItem>, IDisposable
    {
        protected DockPanel panel;
        internal bool main = false;

        public DockItem()
        {
            height = 200D;
            width = 280D;
        }
        
        public DockPanel Panel
        {
            get { return panel; }
            set
            {
                if (panel == value)
                    return;
                panel = value;
                panel.MapItem = this;
            }
        }

        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                if (main)
                    return;

                base.Visible = value;

                if (panel != null)
                    panel.Visible = value;
            }
        }

        public DockItem Add(DockPanel panel)
        {
            var item = new DockItem() { Name = panel.Name, Panel = panel };
            Add(item);
            return item;
        }

        public override void Dispose()
        {
            foreach (var item in this)
                if (item is IDisposable)
                    ((IDisposable)item).Dispose();
            panel?.Dispose();
        }

    }
}

