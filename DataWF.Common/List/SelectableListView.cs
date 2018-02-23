using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;
using System.ComponentModel;

namespace DataWF.Common
{
    public class ListTreeView<T> : SelectableListView<T> where T : IGroup
    {
        QueryParameter groupParam;

        public ListTreeView()
        {
            groupParam = QueryParameter.CreateTreeFilter(typeof(T));
            query.Parameters.Add(groupParam);
        }

        public ListTreeView(IList baseCollection)
                : this()
        {
            SetCollection(baseCollection);
        }
    }

    public class SelectableListView<T> : SelectableList<T>, IFilterable
    {
        [NonSerialized]
        protected ListChangedEventHandler _listChangedHandler;
        [NonSerialized]
        protected List<InvokerComparer> _comparers;
        [NonSerialized]
        protected Query query = new Query();

        [NonSerialized]
        protected IList sourceList;
        [NonSerialized]
        protected ISelectable ssourceList;

        public SelectableListView()
        {
            propertyHandler = null;
            _listChangedHandler = new ListChangedEventHandler(SourceListChanged);
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
                ssourceList.ListChanged -= _listChangedHandler;
            }

            sourceList = baseCollection;
            ssourceList = baseCollection as ISelectable;

            if (ssourceList != null)
            {
                ssourceList.ListChanged += _listChangedHandler;
            }
            Update((IEnumerable<T>)sourceList);
        }

        ~SelectableListView()
        {
            if (ssourceList != null)
                ssourceList.ListChanged -= _listChangedHandler;
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
                AddInternal(item);

            if (comparer != null)
                ListHelper.QuickSort<T>(items, comparer);

            OnListChanged(ListChangedType.Reset, -1);
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

        protected virtual void SourceListChanged(object sender, ListChangedEventArgs e)
        {
            T item = default(T);
            switch (e.ListChangedType)
            {
                case ListChangedType.ItemChanged:
                    item = (T)sourceList[e.NewIndex];
                    int index = IndexOf(item);
                    bool checkItem = ListHelper.CheckItem(item, query);
                    if (index >= 0 && !checkItem)
                    {
                        this.Remove(item, index);
                    }
                    else if (checkItem)
                    {
                        if (index == -1)
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
                                    OnListChanged(ListChangedType.ItemChanged, index);
                                }
                                OnListChanged(ListChangedType.ItemChanged, newindex);
                            }
                            else
                                OnListChanged(ListChangedType.ItemChanged, index);
                        }
                    }

                    break;
                case ListChangedType.ItemDeleted:
                    if (e.NewIndex != -1)
                    {
                        Remove((T)sourceList[e.NewIndex]);
                    }
                    break;
                case ListChangedType.ItemAdded:
                    item = (T)sourceList[e.NewIndex];

                    if (ListHelper.CheckItem(item, query))
                        base.Add(item);
                    break;
                case ListChangedType.Reset:
                    UpdateFilter();
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
