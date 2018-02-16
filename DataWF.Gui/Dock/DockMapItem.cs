using System;
using DataWF.Common;

namespace DataWF.Gui
{
    public class DockMapItem : LayoutItem, IDisposable
    {
        [NonSerialized()]
        protected DockPanel panel;
        internal bool main = false;

        public DockMapItem()
        {
            height = 200D;
            width = 260D;
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

        public void Dispose()
        {
            panel.Dispose();
        }
    }
}

