using System.Collections.Generic;

namespace DataWF.Gui
{
    public class LayoutItemComparer : IComparer<ILayoutItem>
    {
        public int Compare(ILayoutItem x, ILayoutItem y)
        {
            return LayoutMapHelper.Compare(x, y);
        }
    }
}

