using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using System.Text.Json.Serialization;

namespace DataWF.Common
{
    [InvokerGenerator]
    public abstract partial class SynchronizedItem : DefaultItem, ISynchronized
    {
        protected SynchronizedStatus syncStatus = SynchronizedStatus.New;
        protected IWebTableItemList clientContainer = null;

        [Newtonsoft.Json.JsonIgnore, JsonIgnore, XmlIgnore, Browsable(false)]
        public IWebTableItemList ClientContainer => clientContainer ?? (clientContainer = TypeHelper.GetContainers<IWebTableItemList, PropertyChangedEventHandler>(propertyChanged).FirstOrDefault());

        [Newtonsoft.Json.JsonIgnore, JsonIgnore, XmlIgnore, Browsable(false)]
        public IModelSchema Schema => ClientContainer?.Client.Schema;

        [Newtonsoft.Json.JsonIgnore, JsonIgnore, XmlIgnore, Browsable(false)]
        public IModelProvider Provider => Schema?.Provider;

        [Newtonsoft.Json.JsonIgnore, JsonIgnore, XmlIgnore, Browsable(false)]
        public IDictionary<string, object> Changes { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

        [Newtonsoft.Json.JsonIgnore, JsonIgnore, XmlIgnore]
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

        protected override void OnPropertyChanged<T>(T oldValue, T newValue, [CallerMemberName] string propertyName = null)
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
                    if (ListHelper.Equal((T)cacheValue, newValue))
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
    }
}