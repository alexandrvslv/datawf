using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public class ReferenceList<T> : ChangeableList<T>, IReferenceList where T : SynchronizedItem
    {
        //private ISelectable<T> source;

        public ReferenceList()
        {
            Indexes.Concurrent = true;
        }

        public ReferenceList(SynchronizedItem owner, string ownerProperty) : this()
        {
            Owner = owner;
            OwnerProperty = ownerProperty;
        }

        public ReferenceList(SynchronizedItem owner, string ownerProperty, IEnumerable<T> items) : this(owner, ownerProperty)
        {
            AddRange(items);
        }

        [XmlIgnore, JsonIgnore]
        public SynchronizedItem Owner { get; set; }

        public string OwnerProperty { get; set; }

        //[XmlIgnore, JsonIgnore]
        //public ISelectable<T> Source
        //{
        //    get => source;
        //    set
        //    {
        //        if (source == value)
        //            return;
        //        source = value;
        //    }
        //}

        public override void CheckStatus(T item)
        {
            base.CheckStatus(item);
        }

        protected override void OnItemStatusChanged(T item)
        {
            base.OnItemStatusChanged(item);

            switch (Owner.SyncStatus)
            {
                case SynchronizedStatus.New:
                    if (changes.Any())
                    {
                        Owner.Changes[OwnerProperty] = this;
                        Owner.OnPropertyChanged(OwnerProperty);
                    }
                    break;
                case SynchronizedStatus.Actual:
                    if (changes.Any())
                    {
                        Owner.Changes[OwnerProperty] = this;
                        Owner.SyncStatus = SynchronizedStatus.Edit;
                        Owner.OnPropertyChanged(OwnerProperty);
                    }
                    break;
                case SynchronizedStatus.Edit:
                    if (!changes.Any())
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

        [Invoker(typeof(ReferenceList<>), nameof(Owner))]
        public class OwnerInvoker : Invoker<ReferenceList<T>, SynchronizedItem>
        {
            public override string Name => nameof(ReferenceList<T>.Owner);

            public override bool CanWrite => true;

            public override SynchronizedItem GetValue(ReferenceList<T> target) => target.Owner;

            public override void SetValue(ReferenceList<T> target, SynchronizedItem value) { target.Owner = value; }
        }

        [Invoker(typeof(ReferenceList<>), nameof(OwnerProperty))]
        public class OwnerPropertyInvoker : Invoker<ReferenceList<T>, string>
        {
            public override string Name => nameof(ReferenceList<T>.OwnerProperty);

            public override bool CanWrite => true;

            public override string GetValue(ReferenceList<T> target) => target.OwnerProperty;

            public override void SetValue(ReferenceList<T> target, string value) { target.OwnerProperty = value; }
        }
    }
}