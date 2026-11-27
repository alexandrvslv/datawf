using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DataWF.Collections;

public class ChangeableList<T> : SelectableList<T>, IChangeableList<T>
    where T : notnull
{
    protected ConcurrentDictionary<T, SynchronizedStatus> changes = new();

    public event EventHandler<NotifyStatusChangedEventArgs>? ItemStatusChanged;

    public SynchronizedStatus GetStatus(T item)
    {
        return changes.TryGetValue(item, out var status) ? status : SynchronizedStatus.Actual;
    }

    public void CheckStatus(object item)
    {
        CheckStatus((T)item);
    }

    public virtual void CheckStatus(T item)
    {
        var status = GetStatus(item);

        if ((status & SynchronizedStatus.Load) == 0)
        {
            SetStatus(item, status);
        }
    }

    public void SetStatus(T item, SynchronizedStatus status)
    {
        if (status == SynchronizedStatus.Actual)
        {
            if (changes.TryRemove(item, out _))
            {
                OnItemStatusChanged(item);
            }
        }
        else if (changes.TryAdd(item, status))
        {
            OnItemStatusChanged(item);
        }
    }

    protected virtual void OnItemStatusChanged(T item)
    {
        ItemStatusChanged?.Invoke(this, new NotifyStatusChangedEventArgs(item as ISynchronized));
    }

    IEnumerable IChangeableList.GetChanged()
    {
        return changes.Keys;
    }

    public ICollection<T> GetChanged()
    {
        return changes.Keys;
    }

    public override void InsertInternal(int index, T item)
    {
        base.InsertInternal(index, item);
        CheckStatus(item);
    }

    public override void RemoveInternal(T item, int index)
    {
        base.RemoveInternal(item, index);
        if (changes.TryRemove(item, out _))
        {
            OnItemStatusChanged(item);
        }
    }
}