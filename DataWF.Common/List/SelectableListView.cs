using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace DataWF.Common
{
    public class ListTreeView<T> : SelectableListView<T> where T : IGroup
    {
        QueryParameter<T> groupParam;

        public ListTreeView()
        {
            groupParam = GroupHelper.CreateTreeFilter<T>();
            query.Parameters.Add(groupParam);
        }

        public ListTreeView(IList baseCollection) : this()
        {
            SetCollection(baseCollection);
        }
    }

    public class SelectableListView<T> : SelectableList<T>, IFilterable
    {
        protected NotifyCollectionChangedEventHandler _listChangedHandler;
        protected PropertyChangedEventHandler _listItemChangedHandler;
        protected List<InvokerComparer> _comparers;
        protected Query<T> query = new Query<T>();

        protected IList sourceList;
        protected ISelectable ssourceList;

        public SelectableListView()
        {
            propertyHandler = null;
        }

        public SelectableListView(IList baseCollection)
            : this(baseCollection, true)
        { }

        public SelectableListView(IList baseCollection, bool handle)
        {
            if (handle)
            {
                _listChangedHandler = SourceListChanged;
                _listItemChangedHandler = SourceItemChanged;
            }
            SetCollection(baseCollection);
        }

        public void SetCollection(IList baseCollection)
        {
            if (sourceList == baseCollection)
                return;
            if (ssourceList != null && _listChangedHandler != null)
            {
                ssourceList.CollectionChanged -= _listChangedHandler;
                ssourceList.ItemPropertyChanged -= _listItemChangedHandler;
            }

            sourceList = baseCollection;
            ssourceList = baseCollection as ISelectable;

            if (ssourceList != null && _listChangedHandler != null)
            {
                ssourceList.CollectionChanged += _listChangedHandler;
                ssourceList.ItemPropertyChanged += _listItemChangedHandler;
            }
            else
            {
                _listChangedHandler = null;
                _listItemChangedHandler = null;
            }
            Update((IEnumerable<T>)sourceList);
        }

        public Query<T> FilterQuery
        {
            get { return query; }
        }

        public InvokerComparerList SortDescriptions
        {
            get { return null; }
        }

        IQuery IFilterable.FilterQuery => FilterQuery;

        public override object NewItem()
        {
            return ssourceList?.NewItem() ?? base.NewItem();
        }

        public override void Add(T item)
        {
            if (!sourceList.Contains(item))
            {
                sourceList.Add(item);
                if (_listChangedHandler != null)
                {
                    return;
                }
            }


            base.Add(item);
        }

        public void FilterCollection(IEnumerable<T> items)
        {
            ClearInternal();
            AddRange(items);
        }

        protected void Update(IEnumerable list)
        {
            ClearInternal();

            foreach (T item in list)
                InsertInternal(items.Count, item);

            if (comparer != null)
                ListHelper.QuickSort<T>(items, comparer);

            OnListChanged(NotifyCollectionChangedAction.Reset);
        }

        public virtual void UpdateFilter()
        {
            if (sourceList.Count == 0)
            {
                Clear();
            }
            else
            {
                Update(query.Parameters.Count > 0 ? (ssourceList != null ? ssourceList.Select(query) : ListHelper.Select<T>((IList<T>)sourceList, query, null)) : sourceList);
            }
        }

        public virtual void SourceListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            T newItem = e.NewItems == null ? default(T) : e.NewItems.Cast<T>().FirstOrDefault();
            T oldItem = e.OldItems == null ? default(T) : e.OldItems.Cast<T>().FirstOrDefault();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    UpdateFilter();
                    break;
                case NotifyCollectionChangedAction.Move:
                    SourceItemChanged(newItem, new PropertyChangedEventArgs(string.Empty));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Remove(oldItem);
                    goto case NotifyCollectionChangedAction.Add;
                case NotifyCollectionChangedAction.Remove:
                    Remove(oldItem);
                    break;
                case NotifyCollectionChangedAction.Add:
                    if (ListHelper.CheckItem(newItem, query))
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
                Update(sourceList);
            }
        }

        public override void Dispose()
        {
            if (ssourceList != null && _listChangedHandler != null)
            {
                ssourceList.CollectionChanged -= _listChangedHandler;
                ssourceList.ItemPropertyChanged -= _listItemChangedHandler;
            }

            base.Dispose();
        }

    }
}
