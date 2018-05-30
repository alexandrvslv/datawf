using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF.Common;
using Xwt;

namespace DataWF.Gui
{
    public class DockItem : LayoutItem<DockItem>, IDisposable
    {
        protected DockPanel panel;

        private DockBox box;

        public DockItem()
        {
            height = 200D;
            width = 280D;
        }

        [DefaultValue(false)]
        public bool Main { get; set; }

        public DockPanel Panel
        {
            get { return panel; }
            set
            {
                if (panel == value)
                    return;
                panel = value;
                panel.DockItem = this;
                if (box != null && panel.Parent == null)
                {
                    box.Add(Panel);
                }
            }
        }

        [XmlIgnore]
        public DockBox DockBox
        {
            get { return box; }
            set
            {
                if (box == value)
                    return;
                if (box != null && Panel != null)
                {
                    box.Remove(Panel);
                }
                box = value;
                if (box != null && Panel != null)
                {
                    box.Add(Panel);
                }
                foreach (var item in this)
                {
                    item.DockBox = value;
                }
            }
        }

        public override void InsertInternal(int index, DockItem item)
        {
            item.DockBox = DockBox;
            base.InsertInternal(index, item);
        }

        public override bool Visible
        {
            get { return base.Visible; }
            set
            {
                if (Main)
                    return;

                base.Visible = value;

                if (panel != null)
                    panel.Visible = value;
            }
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

