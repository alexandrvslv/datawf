using System.Collections;
using System.Collections.Generic;

namespace DataWF.Common
{
    public interface ITreeComparer : IComparer, IComparer<IGroup>
    {
        IComparer Comparer { get; set; }
    }
}