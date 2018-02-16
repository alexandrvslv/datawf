using System;
using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    /// <summary>
    /// Comparer tree. Buid tree from list of IGroupable objects.
    /// Used IGroupable.Level, IGroupable.TopGroup and IGroupable.Group properties of list items
    /// </summary>
    public class TreeComparer : IComparer<IGroup>, IComparer
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
            if (x is IGroup || y is IGroup)
                return Compare(x as IGroup, y as IGroup);
            return comp.Compare(x, y);
        }

        public int Compare(IGroup x, IGroup y)
        {
            return GroupHelper.Compare(x, y, comp);
        }
        #endregion
    }

}

