using System;
using System.ComponentModel;
using System.Xml.Serialization;
using DataWF;
using Avalonia;

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
            get => panel;
            set
            {
                if (panel == value)
                    return;
                panel = value;
                panel.DockItem = this;
                panel.IsVisible = Visible;
                if (box != null && panel.Parent == null)
                {
                    box.Add(Panel);
                }
            }
        }

        [XmlIgnore]
        public DockBox DockBox
        {
            get => box;
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
            get => base.IsVisible;
            set
            {
                if (Main)
                    return;

                base.IsVisible = value;

                if (panel != null)
                    panel.IsVisible = value;
            }
        }

        [XmlIgnore]
        public Rect Bound { get; private set; }

        public override void ApplyBound(Rect value)
        {
            base.ApplyBound(value);
            Bound = value.Inflate(new Thickness(-3, -3));
        }

        public override void Dispose()
        {
            base.Dispose();
            if (panel != null && panel.Parent == DockBox)
            {
                DockBox?.Remove(panel);
            }
            panel?.Dispose();
        }

    }
}

