using System.Collections.Generic;

namespace DataWF.Collections;

public interface IKeyedCollection<T> : ICollection<T>
{
    T GetByKey(object id);
}

public interface IKeyedCollection<T, K> : IKeyedCollection<T>
{
    T GetByKey(K id);

    T IKeyedCollection<T>.GetByKey(object id) => GetByKey((K)id);
}

