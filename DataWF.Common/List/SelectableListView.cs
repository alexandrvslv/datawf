using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace DataWF.Common
{
    public class SelectableListView<T> : SelectableList<T>, IFilterable<T>
    {
        protected NotifyCollectionChangedEventHandler _listChangedHandler;
        protected PropertyChangedEventHandler _listItemChangedHandler;
        protected Query<T> query;

        protected IEnumerable source;
        protected ISelectable selectableSource;
        private string tempParameters;

        public SelectableListView(Query<T> filter)
        {
            propertyHandler = null;
            FilterQuery = filter;
        }

        public SelectableListView() : this(new Query<T>())
        { }

        public SelectableListView(IEnumerable baseCollection)
            : this(baseCollection, true)
        { }

        public SelectableListView(IEnumerable baseCollection, bool handle)
            : this(new Query<T>(), baseCollection, handle)
        { }

        public SelectableListView(Query<T> filter, IEnumerable baseCollection, bool handle)
            : this(filter)
        {
            if (handle)
            {
                _listChangedHandler = SourceListChanged;
                _listItemChangedHandler = SourceItemChanged;
            }
            SetCollection(baseCollection);
        }

        public IEnumerable Source
        {
            get { return source; }
            set { SetCollection(value); }
        }

        public void SetCollection(IEnumerable baseCollection)
        {
            if (source == baseCollection)
                return;
            if (selectableSource != null && _listChangedHandler != null)
            {
                selectableSource.CollectionChanged -= _listChangedHandler;
                selectableSource.ItemPropertyChanged -= _listItemChangedHandler;
            }

            source = baseCollection;
            selectableSource = baseCollection as ISelectable;

            if (selectableSource != null && _listChangedHandler != null)
            {
                selectableSource.CollectionChanged += _listChangedHandler;
                selectableSource.ItemPropertyChanged += _listItemChangedHandler;
            }
            UpdateFilter();
        }

        public override bool Contains(T item)
        {
            return base.Contains(item);
        }

        public Query<T> FilterQuery
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
                    query = value;
                    if (query != null)
                    {
                        query.ParametersChanged += ParametersChanged;
                        query.OrdersChanged += OrdersChanged;
                    }
                }
            }
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
                    ApplySort((IComparer<T>)FilterQuery.GetComparer());
                }
            }
            else if (args is PropertyChangedEventArgs p)
            {
                if (p.PropertyName == nameof(InvokerComparer<T>.Direction))
                {
                    comparer = null;
                    ApplySort((IComparer<T>)FilterQuery.GetComparer());
                }
            }
        }

        private void ParametersChanged(object sender, EventArgs args)
        {
            if (args is NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Add
                    || e.Action == NotifyCollectionChangedAction.Remove
                    || e.Action == NotifyCollectionChangedAction.Reset)
                {
                    CheckUpdateFilter(sender, args);
                }
            }
            else if (args is PropertyChangedEventArgs p)
            {
                if (p.PropertyName == nameof(QueryParameter<T>.IsEnabled)
                    || p.PropertyName == nameof(QueryParameter<T>.Value)
                    || p.PropertyName == nameof(QueryParameter<T>.Comparer))
                {
                    CheckUpdateFilter(sender, args);
                }
            }
        }

        public InvokerComparerList SortDescriptions
        {
            get { return null; }
        }

        IQuery IFilterable.FilterQuery => FilterQuery;

        public event EventHandler FilterChanged;

        public override object NewItem()
        {
            return selectableSource?.NewItem() ?? base.NewItem();
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
            lock (lockObject)
            {
                try
                {
                    if (selectableSource != null && _listChangedHandler != null)
                    {
                        selectableSource.CollectionChanged -= _listChangedHandler;
                        selectableSource.ItemPropertyChanged -= _listItemChangedHandler;
                    }
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
                    if (selectableSource != null && _listChangedHandler != null)
                    {
                        selectableSource.CollectionChanged += _listChangedHandler;
                        selectableSource.ItemPropertyChanged += _listItemChangedHandler;
                    }
                }
            }
        }

        public virtual void CheckUpdateFilter(object sender, EventArgs e)
        {
            if (!query.Suspending)
            {
                var parameters = query.FormatEnabled();
                if (tempParameters?.Equals(parameters, StringComparison.Ordinal) ?? false)
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
            UpdateInternal(source == null ? null : ListHelper.Select<T>(source.TypeOf<T>(), query, indexes));
            OnListChanged(NotifyCollectionChangedAction.Reset);
        }

        public virtual void SourceListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            T newItem = e.NewItems == null ? default(T) : e.NewItems.TypeOf<T>().FirstOrDefault();
            T oldItem = e.OldItems == null ? default(T) : e.OldItems.TypeOf<T>().FirstOrDefault();
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    UpdateFilter();
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (newItem != null)
                    {
                        SourceItemChanged(newItem, new PropertyChangedEventArgs(string.Empty));
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (oldItem != null)
                    {
                        Remove(oldItem);
                    }
                    goto case NotifyCollectionChangedAction.Add;
                case NotifyCollectionChangedAction.Remove:
                    if (oldItem != null)
                    {
                        Remove(oldItem);
                    }
                    break;
                case NotifyCollectionChangedAction.Add:
                    if (newItem != null && ListHelper.CheckItem(newItem, query))
                    {
                        if (!query.IsEnabled && comparer == null)
                        {
                            base.Add(newItem);
                        }
                        else
                        {
                            base.Add(newItem);
                        }
                    }
                    break;
            }
        }

        public virtual void SourceItemChanged(object sender, PropertyChangedEventArgs e)
        {
            var item = (T)sender;

            var checkItem = ListHelper.CheckItem(item, query);
            if (checkItem)
            {
                if (query.IsGlobalParameter(e.PropertyName))
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
            if (selectableSource != null && _listChangedHandler != null)
            {
                selectableSource.CollectionChanged -= _listChangedHandler;
                selectableSource.ItemPropertyChanged -= _listItemChangedHandler;
            }

            base.Dispose();
        }

    }
}
