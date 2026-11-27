using System;
using System.Collections.Concurrent;

namespace DataWF;

public class TableLoads<T, K> : ITableLoads<T, K>
    where K : struct
{
    private readonly ConcurrentDictionary<K, T> downloads = new();
    private readonly ConcurrentDictionary<string, LoadProgress<T>> loadQueue = new(StringComparer.OrdinalIgnoreCase);

    public void ClearQueue() => loadQueue.Clear();

    public bool ClearLoadCache(string filter) => loadQueue.TryRemove(filter, out _);

    public bool RemoveDownloads(object id) => RemoveDownloads((K)id);

    public bool RemoveDownloads(K id) => downloads.TryRemove(id, out _);

    public object? AddDownloads(object id, object item) => item switch
    {
        T typed => AddDownloads((K)id, typed),
        Func<K, T> func => AddDownloads((K)id, func),
        _ => throw new ArgumentException("Invalid item type", nameof(item))
    };

    public T AddDownloads(K id, T item) => downloads.GetOrAdd(id, item);

    public T AddDownloads(K id, Func<K, T> newItem) => downloads.GetOrAdd(id, newItem);

    public object? GetDownloads(object id) => GetDownloads((K)id);

    public T? GetDownloads(K id) => downloads.TryGetValue(id, out var item) ? item : default;

    public void SetDownloads(object id, object item) => SetDownloads((K)id, (T)item);

    public void SetDownloads(K id, T item) => downloads[id] = item;
}
