using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Collections.Specialized;

namespace DataWF.Common
{
    public class ListTreeView<T> : SelectableListView<T> where T : IGroup
    {
        QueryParameter groupParam;

        public ListTreeView()
        {
            groupParam = QueryParameter.CreateTreeFilter<T>();
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
        protected List<InvokerComparer> _comparers;
        protected Query query = new Query();

        protected IList sourceList;
        protected ISelectable ssourceList;

        public SelectableListView()
        {
            propertyHandler = null;
            _listChangedHandler = new NotifyCollectionChangedEventHandler(SourceListChanged);
        }

        public SelectableListView(IList baseCollection)
            : this()
        {
            SetCollection(baseCollection);
        }

        public void SetCollection(IList baseCollection)
        {
            if (ssourceList != null)
            {
                ssourceList.CollectionChanged -= _listChangedHandler;
            }

            sourceList = baseCollection;
            ssourceList = baseCollection as ISelectable;

            if (ssourceList != null)
            {
                ssourceList.CollectionChanged += _listChangedHandler;
            }
            Update((IEnumerable<T>)sourceList);
        }

        ~SelectableListView()
        {
            if (ssourceList != null)
                ssourceList.CollectionChanged -= _listChangedHandler;
        }

        public override object NewItem()
        {
            return ssourceList?.NewItem() ?? base.NewItem();
        }

        public override void Add(T item)
        {
            if (!sourceList.Contains(item))
                sourceList.Add(item);
            else
                base.Add(item);
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

        protected virtual void SourceListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            T item = default(T);
            if (e is NotifyListPropertyChangedEventArgs property)
            {
                item = (T)property.Item;
            }
            else
            {
                item = e.NewItems != null ? e.NewItems.Cast<T>().FirstOrDefault() : e.OldItems != null ? e.OldItems.Cast<T>().FirstOrDefault() : default(T);
                property = null;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                    if (item == null)
                    {
                        UpdateFilter();
                        return;
                    }
                    int index = IndexOf(item);
                    bool checkItem = ListHelper.CheckItem(item, query);
                    if (index >= 0 && !checkItem)
                    {
                        this.Remove(item, index);
                    }
                    else if (checkItem)
                    {
                        if (index < 0)
                            base.Add(item);
                        else
                        {
                            if (comparer != null)
                            {
                                int newindex = GetIndexBySort(item);
                                if (newindex != index)
                                {
                                    items.RemoveAt(index);
                                    if (newindex > items.Count)
                                        newindex = items.Count;
                                    items.Insert(newindex, item);
                                    OnListChanged(NotifyCollectionChangedAction.Move, item, newindex, property?.Property, index);
                                }
                                OnListChanged(NotifyCollectionChangedAction.Reset, item, newindex, property?.Property);
                            }
                            else
                                OnListChanged(NotifyCollectionChangedAction.Reset, item, index, property?.Property);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Remove(item);
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Replace:
                    if (e.Action == NotifyCollectionChangedAction.Replace)
                    {
                        Remove(e.OldItems[0]);
                    }
                    if (ListHelper.CheckItem(item, query))
                        base.Add(item);
                    break;
            }
        }

        public Query FilterQuery
        {
            get { return query; }
        }

        public virtual void RemoveFilter()
        {
            if (query.Parameters.Count > 0)
            {
                query.Parameters.Clear();
                Update(sourceList);
            }
        }

        public InvokerComparerList SortDescriptions
        {
            get { return null; }
        }

    }
}
