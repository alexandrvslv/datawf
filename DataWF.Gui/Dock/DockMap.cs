using System;
using DataWF.Common;
using System.Collections.Generic;

namespace DataWF.Gui
{
    public class DockMap : LayoutMap, IDisposable
    {
        public DockMap()
        {
            FillHeight = true;
        }

        public DockMapItem Add(DockPanel panel)
        {
            var item = new DockMapItem() { Name = panel.Name, Panel = panel };
            LayoutMapHelper.Add(this, item);
            return item;
        }

        public void GetBound(DockMapItem item)
        {
            LayoutMapHelper.GetBound(this, item, null, null);
        }

        public void Dispose()
        {
            foreach (var item in items)
                if (item is IDisposable)
                    ((IDisposable)item).Dispose();
        }
    }


}

