using DataWF.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DataWF.Gui
{
    public class LayoutNodeList<T> : SelectableList<T> where T : Node, new()
    {
        private int order;
        [NonSerialized()]
        private CategoryList groups;
        private bool sense = true;

        public LayoutNodeList(CategoryList groups)
            : base()
        {
            this.groups = groups;
            Indexes.Add(new Invoker<Node, string>(nameof(Node.Name), (item) => item.Name));
            Indexes.Add(new Invoker<Node, Node>(nameof(Node.Group), (item) => item.Group));
            //this.Indexes.Add("Expand");
            //this.Indexes.Add ("Visible");
        }

        public bool Sense
        {
            get { return sense; }
            set
            {
                if (sense == value)
                    return;
                sense = value;
                if (sense)
                    OnListChanged(ListChangedType.Reset, -1);
            }
        }

        public override void Insert(int index, T item)
        {
            int idexold = -1;
            if (index < items.Count)
                idexold = items[index].Order;
            item.Order = idexold;
            for (int i = index + 1; i < items.Count; i++)
                items[i].Order++;
            base.Insert(index, item);
        }

        public void Reorder(T node)
        {
            //Node old = newIndex < Count ? _items[newIndex] : null;
            //node.order = old == null ? ++index : old.order;
            var nodes = Select(nameof(Node.Group), CompareType.Equal, node.Group).ToList();
            nodes.Sort();
            int nindex = 0;
            foreach (Node n in nodes)
                n.order = nindex++;
        }

        public void Localize()
        {
            foreach (var node in items)
            {
                if (node is ILocalizable)
                    ((ILocalizable)node).Localize();
            }
        }

        public Node Add(string name, string header)
        {
            T node = new T() { Name = name, Text = header };
            Add(node);
            return node;
        }

        public override int AddInternal(T item)
        {
            if (Contains(item))
                return -1;
            if (item.Order == -1)
            {
                item.Order = ++order;
            }
            CheckGrop(item);
            var index = base.AddInternal(item);
            for (int i = 0; i < item.Childs.Count; i++)
            {
                Add((T)item.Childs[i]);
            }
            return index;
        }

        public void CheckGrop(T item)
        {
            if (item.groupName != null && item.group == null)
                item.group = SelectOne(nameof(Node.Name), CompareType.Equal, item.groupName);

            if (item.categoryName != null && item.categoryName == null)
                item.category = groups.SelectOne(nameof(Node.Name), CompareType.Equal, item.categoryName);
        }

        public override bool Remove(T item)
        {
            bool flag = base.Remove(item);
            foreach (Node n in item.Childs)
                Remove(n);
            return flag;
        }

        public T this[string name]
        {
            get { return Find(name); }
        }

        public T Find(string name)
        {
            return Select(nameof(Node.Name), CompareType.Equal, name).FirstOrDefault();
        }

        public override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //
            if (e.PropertyName.Equals(nameof(Node.Expand), StringComparison.OrdinalIgnoreCase))
            {
                OnListChanged(ListChangedType.Reset, -1);
                //for (int i = 0; i < node.Childs.Count; i++)
                //{
                //    var n = node.Childs[i];
                //    this.OnNotifyPropertyChanged(n, e);
                //    base.OnNotifyPropertyChanged(n, e);
                //}
            }
            else if (e.PropertyName.Equals(nameof(Node.Visible), StringComparison.OrdinalIgnoreCase))
            {
                OnListChanged(ListChangedType.Reset, -1);
                //base.OnNotifyPropertyChanged(sender, e);
                //for (int i = 0; i < node.Childs.Count; i++)
                //{
                //    var n = node.Childs[i];
                //    this.OnNotifyPropertyChanged(n, e);
                //}
            }
            else if (e.PropertyName.Equals(nameof(Node.Group), StringComparison.OrdinalIgnoreCase))
            {
                var lindex = indexes.GetIndex(e.PropertyName);
                if (lindex != null)
                {
                    lindex.Remove((T)sender);
                    lindex.Add((T)sender);
                }
                OnListChanged(ListChangedType.Reset, -1);
            }
            //else
            {
                base.OnPropertyChanged(sender, e);
            }
        }

        public IEnumerable<T> GetTopLevel()
        {
            return Select(nameof(Node.Group), CompareType.Equal, null);
        }

    }
}

