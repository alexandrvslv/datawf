using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

[assembly: Invoker(typeof(SynchronizedItem), nameof(SynchronizedItem.SyncStatus), typeof(SynchronizedItem.SyncStatusInvoker<>))]
namespace DataWF.Common
{
    public abstract class SynchronizedItem : DefaultItem, ISynchronized
    {
        protected SynchronizedStatus syncStatus = SynchronizedStatus.New;

        [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, XmlIgnore, Browsable(false)]
        public IDictionary<string, object> Changes { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

        [Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, XmlIgnore]
        public virtual SynchronizedStatus SyncStatus
        {
            get => syncStatus;
            set
            {
                if (syncStatus != value)
                {
                    syncStatus = value;
                    if (syncStatus == SynchronizedStatus.Actual)
                    {
                        Changes.Clear();
                    }
                    if (syncStatus == SynchronizedStatus.Actual
                        || syncStatus == SynchronizedStatus.Edit
                        || syncStatus == SynchronizedStatus.New)
                    {
                        foreach (var container in Containers)
                        {
                            if (container is IChangeableList changableList)
                            {
                                changableList.CheckStatus(this);
                            }
                        }
                    }
                }
            }
        }

        public bool RejectChanges()
        {
            var flag = false;
            foreach (var entry in Changes.ToList())
            {
                var sflag = RejectChange(entry.Key, entry.Value);
                if (sflag)
                {
                    flag = true;
                }
            }
            return flag;
        }

        public bool RejectChange(string property)
        {
            if (Changes.TryGetValue(property, out var oldValue))
            {
                return RejectChange(property, oldValue);
            }
            return false;
        }

        public bool RejectChange(string property, object oldValue)
        {
            var invoker = EmitInvoker.Initialize(GetType(), property);
            if (invoker != null)
            {
                invoker.SetValue(this, oldValue);
                return true;
            }
            return false;
        }

        protected override void OnPropertyChanged(object oldValue, object newValue, [CallerMemberName] string propertyName = null)
        {
            if (syncStatus == SynchronizedStatus.Actual)
            {
                SyncStatus = SynchronizedStatus.Edit;
            }
            if (syncStatus != SynchronizedStatus.Load)
            {
                if (!Changes.TryGetValue(propertyName, out var cacheValue))
                {
                    Changes[propertyName] = oldValue;
                }
                else
                {
                    if (ListHelper.Equal(cacheValue, newValue))
                    {
                        Changes.Remove(propertyName);
                        if (syncStatus == SynchronizedStatus.Edit
                            && Changes.Count == 0)
                        {
                            SyncStatus = SynchronizedStatus.Actual;
                        }
                    }
                }
            }

            base.OnPropertyChanged(oldValue, newValue, propertyName);
        }

        public virtual bool PropertyChangeOverride(string propertyName)
        {
            return false;
        }

        public class SyncStatusInvoker<T> : Invoker<T, SynchronizedStatus> where T : SynchronizedItem
        {
            public override string Name => nameof(SynchronizedItem.SyncStatus);

            public override bool CanWrite => true;

            public override SynchronizedStatus GetValue(T target) => target.SyncStatus;

            public override void SetValue(T target, SynchronizedStatus value) => target.SyncStatus = value;
        }
    }
}