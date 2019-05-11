using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace DataWF.Common
{
    public class ReferenceList<T> : SelectableList<T> where T : SynchronizedItem
    {
        public ReferenceList()
        { }

        public ReferenceList(SynchronizedItem owner, string ownerProperty, IList<T> baseCollection)
        {
            Owner = owner;
            OwnerProperty = ownerProperty;
            BaseCollection = baseCollection;
        }

        public SynchronizedItem Owner { get; set; }
        public string OwnerProperty { get; set; }
        public IList<T> BaseCollection { get; set; }

        public override void OnListChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnListChanged(e);
            CheckOwnerStatus();
        }

        public override void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemPropertyChanged(sender, e);
            if (e.PropertyName == nameof(SynchronizedItem.SyncStatus))
            {
                CheckOwnerStatus();
            }
        }

        public override int Add(T item)
        {
            var index = base.Add(item);
            if (index >= 0 && !BaseCollection.Contains(item))
            {
                BaseCollection.Add(item);
            }
            return index;
        }

        private void CheckOwnerStatus()
        {
            switch (Owner.SyncStatus)
            {
                case SynchronizedStatus.Actual:
                    if (this.Any(p => p.SyncStatus != SynchronizedStatus.Actual))
                    {
                        Owner.SyncStatus = SynchronizedStatus.Edit;
                        Owner.Changes[OwnerProperty] = this;
                    }
                    break;
                case SynchronizedStatus.Edit:
                    if (!this.Any(p => p.SyncStatus != SynchronizedStatus.Actual))
                    {
                        if (Owner.Changes.Remove(OwnerProperty) && Owner.Changes.Count == 0)
                        {
                            Owner.SyncStatus = SynchronizedStatus.Actual;
                        }
                    }
                    break;
            }
        }
    }
}