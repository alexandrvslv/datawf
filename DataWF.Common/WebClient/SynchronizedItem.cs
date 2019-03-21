﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace DataWF.Common
{
    public enum SynchronizedStatus
    {
        New,
        Edit,
        Load,
        Actual
    }

    public abstract class SynchronizedItem : DefaultItem, ISynchronized
    {
        private SynchronizedStatus syncStatus = SynchronizedStatus.New;

        [JsonIgnore, XmlIgnore]
        public IDictionary<string, object> Changes { get; } = new Dictionary<string, object>();

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
                }
            }
        }

        protected override void OnPropertyChangingValue(object oldValue, [CallerMemberName] string propertyName = null)
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
            }

            OnPropertyChanging(propertyName);
        }

        protected override void OnPropertyChangedValue(object newValue, [CallerMemberName] string propertyName = null)
        {
            if (syncStatus != SynchronizedStatus.Load)
            {
                if (Changes.TryGetValue(propertyName, out var cacheValue))
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

            OnPropertyChanged(propertyName);
        }
    }


}