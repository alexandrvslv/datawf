using System.Collections;

namespace DataWF.Collections
{
    public interface ISorted
    {
        int BinarySearch(object item, IComparer comparer);
    }
}



