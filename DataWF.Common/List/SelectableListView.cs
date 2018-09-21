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

        public SelectableListView()
        {
            propertyHandler = null;
            FilterQuery = new Query<T>();
        }

        public SelectableListView(IEnumerable baseCollection)
            : this(baseCollection, true)
        {
        }

        public SelectableListView(IEnumerable baseCollection, bool handle)
            : this()
        {
            if (handle)
            {
                _listChangedHandler = SourceListChanged;
                _listItemChangedHandler = SourceItemChanged;
            }
            SetCollection(baseCollection);
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
            else
            {
                _listChangedHandler = null;
                _listItemChangedHandler = null;
            }
            UpdateInternal(source.TypeOf<T>());
            OnListChanged(NotifyCollectionChangedAction.Reset);
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
                        query.Parameters.ItemPropertyChanged -= FilterPropertyChanged;
                        query.Parameters.CollectionChanged -= FilterCollectionChanged;
                        query.Orders.ItemPropertyChanged -= FilterPropertyChanged;
                        query.Orders.CollectionChanged -= OrdersCollectionChanged;
                    }
                    query = value;
                    if (query != null)
                    {
                        query.Parameters.ItemPropertyChanged += FilterPropertyChanged;
                        query.Parameters.CollectionChanged += FilterCollectionChanged;
                        query.Orders.ItemPropertyChanged += FilterPropertyChanged;
                        query.Orders.CollectionChanged += OrdersCollectionChanged;
                    }
                }
            }
        }

        private void OrdersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add
                || e.Action == NotifyCollectionChangedAction.Remove
                || e.Action == NotifyCollectionChangedAction.Reset)
            {
                comparer = null;
                ApplySort((IComparer<T>)FilterQuery.GetComparer());
            }
        }

        private void FilterCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add
                || e.Action == NotifyCollectionChangedAction.Remove
                || e.Action == NotifyCollectionChangedAction.Reset)
            {
                UpdateFilter();
            }
        }

        private void FilterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(QueryParameter<T>.IsEnabled):
                case nameof(QueryParameter<T>.Value):
                    UpdateFilter();
                    break;
                case nameof(InvokerComparer<T>.Direction):
                    comparer = null;
                    ApplySort((IComparer<T>)FilterQuery.GetComparer());
                    break;
            }
        }

        public InvokerComparerList SortDescriptions
        {
            get { return null; }
        }

        IQuery IFilterable.FilterQuery => FilterQuery;

        public override object NewItem()
        {
            return selectableSource?.NewItem() ?? base.NewItem();
        }

        public override void Add(T item)
        {
            if (selectableSource != null && !selectableSource.Contains(item))
            {
                selectableSource.Add(item);
                if (_listChangedHandler != null)
                {
                    return;
                }
            }

            base.Add(item);
        }

        protected void UpdateInternal(IEnumerable<T> list)
        {
            ClearInternal();

            foreach (T item in list)
                InsertInternal(items.Count, item);

            if (comparer == null || comparer is InvokerComparerList<T>)
            {
                var newComparer = FilterQuery.GetComparer();
                ApplySortInternal(newComparer);
            }

        }

        public virtual void UpdateFilter()
        {
            UpdateInternal(ListHelper.Select<T>(source.TypeOf<T>(), query, selectableSource is ISelectable<T> gSelectable ? gSelectable.Indexes : null));
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
                        base.Add(newItem);
                    }
                    break;
            }
        }

        public virtual void SourceItemChanged(object sender, PropertyChangedEventArgs e)
        {
            var item = (T)sender;
            int index = IndexOf(item);
            bool checkItem = ListHelper.CheckItem(item, query);
            if (checkItem)
            {
                if (index < 0)
                {
                    base.Add(item);
                }
                else
                {
                    base.OnItemPropertyChanged(item, index, e);
                }
            }
            else if (index >= 0)
            {
                Remove(item, index);
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
