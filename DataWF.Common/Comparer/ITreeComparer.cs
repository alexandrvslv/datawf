using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface ITreeComparer : IComparer
    {
        IComparer Comparer { get; set; }
    }

    public interface ITreeComparer<T> : ITreeComparer, IComparer<T> where T : IGroup
    {
        new IComparer<T> Comparer { get; set; }
    }
}