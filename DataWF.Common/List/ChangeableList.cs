using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DataWF.Common
{
    public class ChangeableList<T> : SelectableList<T>, IChangeableList<T>
    {
        protected ConcurrentDictionary<T, byte> changes = new ConcurrentDictionary<T, byte>();

        public event EventHandler<NotifyStatusChangedEventArgs> ItemStatusChanged;

        public void CheckStatus(object item)
        {
            CheckStatus((T)item);
        }

        public virtual void CheckStatus(T item)
        {
            var syncItem = item as ISynchronized;
            if (syncItem.SyncStatus == SynchronizedStatus.Actual)
            {
                if (changes.TryRemove(item, out _))
                {
                    OnItemStatusChanged(item);
                }
            }
            else if (syncItem.SyncStatus == SynchronizedStatus.New
                || syncItem.SyncStatus == SynchronizedStatus.Edit)
            {
                if (changes.TryAdd(item, 0))
                {
                    OnItemStatusChanged(item);
                }
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
}