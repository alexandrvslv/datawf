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

        public ListTreeView(IList baseCollection) : this()
        {
            SetCollection(baseCollection);
        }
    }

    public class SelectableListView<T> : SelectableList<T>, IFilterable
    {
        protected ListChangedEventHandler _listChangedHandler;
        protected List<InvokerComparer> _comparers;
        protected Query query = new Query();

        protected IList sourceList;
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
                InsertInternal(items.Count, item);

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
            T item = e is ListPropertyChangedEventArgs ? (T)((ListPropertyChangedEventArgs)e).Sender : e.NewIndex != -1 ? (T)sourceList[e.NewIndex] : default(T);
            switch (e.ListChangedType)
            {
                case ListChangedType.ItemChanged:
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
                                    OnListChanged(ListChangedType.ItemChanged, index, -1, item);
                                }
                                OnListChanged(ListChangedType.ItemChanged, newindex, -1, item);
                            }
                            else
                                OnListChanged(ListChangedType.ItemChanged, index, -1, item);
                        }
                    }
                    break;
                case ListChangedType.ItemDeleted:
                    Remove(item);
                    break;
                case ListChangedType.ItemAdded:
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
