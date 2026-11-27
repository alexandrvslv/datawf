using System.Collections.Generic;

namespace DataWF.Collections;

public interface ISorted<T>
{
    int BinarySearch(T item, IComparer<T> comparer);
}



