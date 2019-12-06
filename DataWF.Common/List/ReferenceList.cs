using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace DataWF.Common
{
    public class ReferenceList<T> : SelectableList<T>, IReferenceList where T : SynchronizedItem
    {
        public ReferenceList()
        {
            Indexes.Concurrent = true;
        }

        public ReferenceList(SynchronizedItem owner, string ownerProperty) : this()
        {
            Owner = owner;
            OwnerProperty = ownerProperty;
        }

        public SynchronizedItem Owner { get; set; }
        public string OwnerProperty { get; set; }

        public override void OnListChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnListChanged(e);
            IList items = e.Action == NotifyCollectionChangedAction.Add
                ? e.NewItems
                : e.Action == NotifyCollectionChangedAction.Reset
                || e.Action == NotifyCollectionChangedAction.Remove
                ? (IList)this.items
                : null;
            if (items != null)
            {
                CheckOwnerStatus(items);
            }
        }

        public override int Add(T item)
        {
            if (Contains(item))
                return -1;
            return base.Add(item);
        }

        public void CheckOwnerStatus(IEnumerable items)
        {
            switch (Owner.SyncStatus)
            {
                case SynchronizedStatus.New:
                    if (items.Cast<T>().Any(p => p.SyncStatus == SynchronizedStatus.New
                    || p.SyncStatus == SynchronizedStatus.Edit))
                    {
                        Owner.Changes[OwnerProperty] = this;
                        Owner.OnPropertyChanged(OwnerProperty);
                    }
                    break;
                case SynchronizedStatus.Actual:
                    if (items.Cast<T>().Any(p => p.SyncStatus == SynchronizedStatus.New
                    || p.SyncStatus == SynchronizedStatus.Edit))
                    {
                        Owner.SyncStatus = SynchronizedStatus.Edit;
                        Owner.Changes[OwnerProperty] = this;
                        Owner.OnPropertyChanged(OwnerProperty);
                    }
                    break;
                case SynchronizedStatus.Edit:
                    if (!this.Any(p => p.SyncStatus != SynchronizedStatus.Actual
                    && p.SyncStatus != SynchronizedStatus.Load))
                    {
                        if (Owner.Changes.Remove(OwnerProperty) && Owner.Changes.Count == 0)
                        {
                            Owner.SyncStatus = SynchronizedStatus.Actual;
                            Owner.OnPropertyChanged(OwnerProperty);
                        }
                    }
                    else
                    {
                        Owner.Changes[OwnerProperty] = this;
                        Owner.OnPropertyChanged(OwnerProperty);
                    }
                    break;
            }
        }
    }
}