using System;
using DataWF.Common;
using System.Collections.Generic;

namespace DataWF.Gui
{
    public class DockPageList : SelectableList<DockPage>
    {
        [NonSerialized()]
        private DockPageBox box;

        public DockPageList(DockPageBox box)
            : base()
        {
            this.box = box;
        }

        public DockPageBox Box
        {
            get { return box; }
        }

        public DockPage this[string name]
        {
            get { return SelectOne(nameof(DockPage.Name), CompareType.Equal, name); }
        }
        
        public override void Add(DockPage item)
        {
            item.List = this;
            base.Add(item);
        }

        public override void Dispose()
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item.Widget != null)
                    {
                        try
                        {
                            item.Widget.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Helper.OnException(ex);
                        }
                    }
                }
            }
            base.Dispose();
        }
    }
}
