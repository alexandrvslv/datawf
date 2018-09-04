using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    /// <summary>
    /// Comparer tree. Buid tree from list of IGroupable objects.
    /// Used IGroupable.Level, IGroupable.TopGroup and IGroupable.Group properties of list items
    /// </summary>
    public class TreeComparer<T> : IComparer<T>, IComparer where T : IGroup
    {
        IComparer comp;

        public TreeComparer()
        {
        }

        public TreeComparer(IComparer comparer)
        {
            this.comp = comparer;
        }

        public IComparer Comparer
        {
            get { return comp; }
        }

        #region IComparer Members

        public int Compare(object x, object y)
        {
            return Compare((T)x, (T)y);
        }

        public int Compare(T x, T y)
        {
            return GroupHelper.Compare(x, y, comp);
        }
        #endregion
    }

}

