using DataWF.Comparers;
using DataWF.Collections.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace DataWF.Collections
{
    public class SelectableListView<T> : SelectableList<T>, IFilterable<T>
    {
        protected NotifyCollectionChangedEventHandler _listChangedHandler;
        protected PropertyChangedEventHandler _listItemChangedHandler;
        protected QQuery<T> query;

        protected IEnumerable source;
        protected ISelectable selectableSource;
        private string tempParameters;
        private int updatingFilter;

        public SelectableListView(QQuery<T> filter)
        {
            propertyHandler = null;
            FilterQuery = filter;
        }

        public SelectableListView() : this(new QQuery<T>())
        { }

        public SelectableListView(IEnumerable baseCollection)
            : this(baseCollection, true)
        { }

        public SelectableListView(IEnumerable baseCollection, bool handle)
            : this(new QQuery<T>(), baseCollection, handle)
        { }

        public SelectableListView(QQuery<T> filter, IEnumerable baseCollection, bool handle)
            : this(filter)
        {
            if (handle)
            {
                _listChangedHandler = OnSourceCollectionChanged;
                _listItemChangedHandler = OnSourceItemChanged;
            }
            Source = baseCollection;
        }

        public IEnumerable Source
        {
            get { return source; }
            set
            {
                if (source == value
                    || this == value)
                {
                    return;
                }
                SuspendHandling();
                source = value;
                selectableSource = value as ISelectable;
                UpdateFilter();
            }
        }

        public IQQuery<T> FilterQuery
        {
            get { return query; }
            set
            {
                if (query != value)
                {
                    if (query != null)
                    {
                        query.ParametersChanged -= ParametersChanged;
                        query.OrdersChanged -= OrdersChanged;
                    }
                    query = (QQuery<T>)value;
                    if (query != null)
                    {
                        query.ParametersChanged += ParametersChanged;
                        query.OrdersChanged += OrdersChanged;
                    }
                }
            }
        }

        public InvokerComparerList SortDescriptions => null;

        IQQuery IFilterable.FilterQuery
        {
            get => FilterQuery;
            set => FilterQuery = (QQuery<T>)value;
        }

        public event EventHandler FilterChanged;

        private void ResumeHandling()
        {
            if (selectableSource != null && _listChangedHandler != null)
            {
                selectableSource.CollectionChanged += _listChangedHandler;
                selectableSource.ItemPropertyChanged += _listItemChangedHandler;
            }
        }

        private void SuspendHandling()
        {
            if (selectableSource != null && _listChangedHandler != null)
            {
                selectableSource.CollectionChanged -= _listChangedHandler;
                selectableSource.ItemPropertyChanged -= _listItemChangedHandler;
            }
        }

        public override bool Contains(T item)
        {
            return base.Contains(item);
        }

        private void OrdersChanged(object sender, EventArgs args)
        {
            if (args is NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Add
                    || e.Action == NotifyCollectionChangedAction.Remove
                    || e.Action == NotifyCollectionChangedAction.Reset)
                {
                    comparer = null;
                    ApplySort((IComparer<T>)FilterQuery.GetComparer<T>());
                }
            }
            else if (args is PropertyChangedEventArgs p)
            {
                if (string.Equals(p.PropertyName, nameof(IInvokerComparer.Direction), StringComparison.Ordinal))
                {
                    comparer = null;
                    ApplySort((IComparer<T>)FilterQuery.GetComparer<T>());
                }
            }
            else
            {
                comparer = null;
                ApplySort((IComparer<T>)FilterQuery.GetComparer<T>());
            }
        }

        private void ParametersChanged(object? sender, EventArgs args)
        {
            if (args is NotifyCollectionChangedEventArgs e)
            {
                var flag = false;
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (IQParameter item in e.NewItems!)
                    {
                        flag |= item.IsEnabled;
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (IQParameter item in e.OldItems!)
                    {
                        flag |= item.IsEnabled;
                    }
                }
                if (e.Action == NotifyCollectionChangedAction.Reset || flag)
                {
                    CheckUpdateFilter(sender, args);
                }
            }
            else if (args is PropertyChangedEventArgs p)
            {
                if (string.Equals(p.PropertyName, nameof(IQParameter.IsEnabled), StringComparison.Ordinal)
                    || string.Equals(p.PropertyName, nameof(IQParameter.RightValue), StringComparison.Ordinal)
                    || string.Equals(p.PropertyName, nameof(IQParameter.CompareOperator), StringComparison.Ordinal))
                {
                    CheckUpdateFilter(sender, args);
                }
            }
        }

        public override T NewItem()
        {
            return (selectableSource?.NewItem() as T) ?? base.NewItem();
        }

        public override int Add(T item)
        {
            if (selectableSource != null)
            {
                if (!selectableSource.Contains(item))
                {
                    selectableSource.Add(item);
                }
                if (_listChangedHandler != null)
                {
                    return -1;
                }
            }

            return base.Add(item);
        }

        protected void UpdateInternal(IEnumerable<T> list)
        {
            if (Interlocked.CompareExchange(ref updatingFilter, 1, 0) != 0)
                return;
            lock (lockObject)
            {
                try
                {
                    SuspendHandling();
                    ClearInternal();
                    if (list == null)
                    {
                        return;
                    }
                    foreach (T item in list)
                    {
                        InsertInternal(items.Count, item);
                    }

                    if (FilterQuery.Orders.Count > 0)
                    {
                        var newComparer = FilterQuery.GetComparer();
                        ApplySortInternal(newComparer);
                    }
                    else if (comparer != null)
                    {
                        ApplySortInternal(comparer);
                    }
                }
                finally
                {
                    ResumeHandling();
                    Interlocked.Decrement(ref updatingFilter);
                }
            }
        }

        public virtual void CheckUpdateFilter(object sender, EventArgs e)
        {
            if (!query.Suspending)
            {
                var parameters = query.FormatEnabled();
                if (string.Equals(tempParameters, parameters, StringComparison.Ordinal))
                {
                    return;
                }
                else
                {
                    tempParameters = parameters;
                }
                UpdateFilter();
            }
            FilterChanged?.Invoke(sender, e);
        }

        public virtual void UpdateFilter()
        {
            var indexes = selectableSource is ISelectable<T> gSelectable ? gSelectable.Indexes : null;
            UpdateInternal(source == null ? null : query.Select(source.TypeOf<T>(), indexes));
            OnCollectionChanged(NotifyCollectionChangedAction.Reset);
        }

        public virtual void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (updatingFilter == 1 || FilterQuery.Suspending)
            {
                return;
            }
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    UpdateFilter();
                    break;
                case NotifyCollectionChangedAction.Move:
                    foreach (T newItem in e.NewItems)
                    {
                        if (newItem != null)
                        {
                            OnSourceItemChanged(newItem, new PropertyChangedEventArgs(string.Empty));
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (T oldItem in e.OldItems)
                    {
                        if (oldItem != null)
                        {
                            Remove(oldItem);
                        }
                    }
                    goto case NotifyCollectionChangedAction.Add;
                case NotifyCollectionChangedAction.Remove:
                    var removeList = new List<T>();
                    foreach (T oldItem in e.OldItems)
                    {
                        if (updatingFilter == 1)
                            return;
                        if (oldItem != null)
                        {
                            Remove(oldItem);
                        }
                    }
                    RemoveRange(removeList);
                    break;
                case NotifyCollectionChangedAction.Add:
                    var addList = new List<T>();
                    foreach (T newItem in e.NewItems)
                    {
                        if (updatingFilter == 1)
                            return;
                        if (newItem != null
                            && (query == null || query.CheckItem(newItem))
                            && GetIndexBySort(newItem) < 0)
                        {
                            addList.Add(newItem);
                        }
                    }
                    if (addList.Count > 0)
                    {
                        AddRange(addList, false);
                    }
                    break;
            }
        }

        public virtual void OnSourceItemChanged(object sender, PropertyChangedEventArgs e)
        {
            if (updatingFilter == 1 || FilterQuery.Suspending)
            {
                return;
            }

            var item = (T)sender;

            var checkItem = query.CheckItem(item);
            if (checkItem)
            {
                if (CheckIsGlobal(e))
                {
                    UpdateFilter();
                }
                else
                {
                    int index = IndexOf(item);
                    if (index < 0)
                    {
                        base.Add(item);
                    }
                    else
                    {
                        base.OnItemPropertyChanged(item, index, e);
                    }
                }
            }
            else
            {
                int index = IndexOf(item);
                if (index >= 0)
                {
                    Remove(item, index);
                }
            }
        }

        private bool CheckIsGlobal(PropertyChangedEventArgs e)
        {
            if (!query.IsEnabled)
                return false;
            if (e is PropertyChangedAggregateEventArgs aggregator)
            {
                foreach (var entry in aggregator.Items)
                {
                    if (query.IsGlobalParameter(entry.PropertyName))
                        return true;
                }
                return false;
            }
            else
            {
                return query.IsGlobalParameter(e.PropertyName);
            }
        }

        public virtual void RemoveFilter()
        {
            if (query.Parameters.Count > 0)
            {
                query.Parameters.Clear();
                UpdateInternal(source.TypeOf<T>());
            }
        }

        public override void Dispose()
        {
            SuspendHandling();

            base.Dispose();
        }

    }
}
