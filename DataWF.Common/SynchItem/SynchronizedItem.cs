using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public enum SynchronizedStatus
    {
        New,
        Edit,
        Load,
        Actual,
        Suspend
    }

    public abstract class SynchronizedItem : DefaultItem, ISynchronized
    {
        private SynchronizedStatus syncStatus = SynchronizedStatus.New;

        [JsonIgnore, XmlIgnore, Browsable(false)]
        public IDictionary<string, object> Changes { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

        [JsonIgnore, XmlIgnore]
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
                    //PERFORMANCE ListView Update(( OnPropertyChanged();
                }
            }
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
                    if (ListHelper.Equal(cacheValue, newValue, false))
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

        [Invoker(typeof(SynchronizedItem), nameof(SynchronizedItem.SyncStatus))]
        public class SyncStatusInvoker<T> : Invoker<T, SynchronizedStatus> where T : SynchronizedItem
        {
            public override string Name => nameof(SynchronizedItem.SyncStatus);

            public override bool CanWrite => true;

            public override SynchronizedStatus GetValue(T target) => target.SyncStatus;

            public override void SetValue(T target, SynchronizedStatus value) => target.SyncStatus = value;
        }
    }
}