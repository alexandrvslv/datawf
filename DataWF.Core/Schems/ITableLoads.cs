using System;

namespace DataWF;

public interface ITableLoads
{
    object AddDownloads(object id, object item);
    bool ClearLoadCache(string filter);
    void ClearQueue();
    object? GetDownloads(object id);
    bool RemoveDownloads(object id);
    void SetDownloads(object id, object item);
}

public interface ITableLoads<T, K>
    where K : struct
{
    T AddDownloads(K id, T item);
    T AddDownloads(K id, Func<K, T> newItem);
    T? GetDownloads(K id);
    bool RemoveDownloads(K id);
    void SetDownloads(K id, T item);
}
