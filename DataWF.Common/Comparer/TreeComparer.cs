using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace DataWF.Common
{
    /// <summary>
    /// Comparer tree. Buid tree from list of IGroupable objects.
    /// Used IGroupable.Level, IGroupable.TopGroup and IGroupable.Group properties of list items
    /// </summary>
    public class TreeComparer<T> : InvokerComparer<T, bool>, ITreeComparer where T : IGroup
    {
        public TreeComparer()
        { }

        public TreeComparer(IComparer comparer)
        {
            Comparer = comparer;
        }

        public IComparer Comparer { get; set; }

        #region IComparer Members

        public override int Compare(T x, T y)
        {
            return GroupHelper.Compare(x, y, Comparer);
        }

        public int Compare(IGroup x, IGroup y)
        {
            return GroupHelper.Compare(x, y, Comparer);
        }
        #endregion
    }

}

