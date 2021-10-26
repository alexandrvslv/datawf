using System.Collections;
using System.Collections.Generic;

namespace DataWF.Gui
{
    public class LayoutItemComparer<T> : IComparer<T> where T : LayoutItem<T>, new()
    {
        public int Compare(T x, T y)
        {
            return LayoutItem<T>.Compare(x, y);
        }

        public int Compare(object x, object y)
        {
            return Compare((ILayoutItem)x, (ILayoutItem)y);
        }
    }
}

