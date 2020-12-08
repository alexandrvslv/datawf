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
        public static readonly TreeComparer<T> Default = new Common.TreeComparer<T>();
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
            return GroupHelper.Compare(x, y, (IComparer<T>)Comparer);
        }

        public override int Compare(object x, object y)
        {
            return Compare((T)x, (T)y);
        }

        int IComparer<IGroup>.Compare(IGroup x, IGroup y)
        {
            return Compare((T)x, (T)y);
        }

        #endregion
    }

}

