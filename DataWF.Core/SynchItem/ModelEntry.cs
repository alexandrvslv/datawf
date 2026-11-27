using DataWF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using DataWF.Models;
using DataWF.Invokers;
using DataWF.Collections;
using DataWF.Comparers;
using DataWF.Serialization;

namespace DataWF
{
    [Invoker]
    public abstract partial class ModelEntry : DefaultItem, ISynchronized, IModelTableContent
    {
        protected SynchronizedStatus syncStatus = SynchronizedStatus.New;
        protected IModelTableItemList? container = null;

        [ElementSerializer(Ignore = true), Browsable(false)]
        public IModelTableItemList? Container => container ??= EventDelegateExtensions.GetContainers<IModelTableItemList, PropertyChangedEventHandler>(propertyChanged)?.FirstOrDefault();

        [ElementSerializer(Ignore = true), Browsable(false)]
        public IModelTable Table
        {
            get => Container?.Table;
            set => throw new NotSupportedException("Depend on Container");
        }

        [ElementSerializer(Ignore = true), Browsable(false)]
        public IModelSchema Schema => Table.Schema;

        [ElementSerializer(Ignore = true), Browsable(false)]
        public IModelProvider Provider => Schema?.Provider;

        [ElementSerializer(Ignore = true), Browsable(false)]
        public IDictionary<string, object> Changes { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

        [ElementSerializer(Ignore = true)]
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
                    if ((syncStatus & SynchronizedStatus.Load) == 0)
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
            var invoker = InvokeProvider.GetInvoker(GetType(), property);
            if (invoker != null)
            {
                invoker.SetValue(this, oldValue);
                return true;
            }
            return false;
        }

        protected override void OnPropertyChanged<T>(T oldValue, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (syncStatus == SynchronizedStatus.Actual)
            {
                SyncStatus = SynchronizedStatus.Edit;
            }
            if (syncStatus != SynchronizedStatus.Load)
            {
                if (!Changes.TryGetValue(propertyName ?? string.Empty, out var cacheValue))
                {
                    Changes[propertyName ?? string.Empty] = oldValue;
                }
                else
                {
                    if (DefaultEqualityComparer<T>.Default.Equals((T)cacheValue, newValue))
                    {
                        Changes.Remove(propertyName ?? string.Empty);
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