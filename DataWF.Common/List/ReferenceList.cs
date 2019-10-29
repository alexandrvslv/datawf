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
            var items = e.Action == NotifyCollectionChangedAction.Add
                ? Enumerable.Repeat((T)e.NewItems[0], 1)
                : e.Action == NotifyCollectionChangedAction.Reset
                || e.Action == NotifyCollectionChangedAction.Remove
                ? (IEnumerable<T>)this
                : null;
            if (items != null)
            {
                CheckOwnerStatus(items);
            }
        }

        public override void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemPropertyChanged(sender, e);
            if (sender is T item && item.SyncStatus != SynchronizedStatus.Load)
            {
                CheckOwnerStatus(Enumerable.Repeat(item, 1));
            }
        }

        public override int Add(T item)
        {
            if (Contains(item))
                return -1;
            return base.Add(item);
        }

        private void CheckOwnerStatus(IEnumerable<T> items)
        {
            switch (Owner.SyncStatus)
            {
                case SynchronizedStatus.Actual:
                    if (items.Any(p => p.SyncStatus == SynchronizedStatus.New
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
                    break;
            }
        }
    }
}