using DataWF.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace DataWF.Gui
{
    public class LayoutNodeList<T> : SelectableList<T> where T : Node, new()
    {
        static readonly Invoker<Node, string> nameInvoker = new Invoker<Node, string>(nameof(Node.Name), item => item.Name);
        static readonly Invoker<Node, Node> groupInvoker = new Invoker<Node, Node>(nameof(Node.Group), item => item.Group);
        private int order;
        private bool sense = true;

        public LayoutNodeList() : base()
        {
            Indexes.Add(nameInvoker);
            Indexes.Add(groupInvoker);
            //this.Indexes.Add("Expand");
            //this.Indexes.Add ("Visible");
        }

        public LayoutNodeList(CategoryList categories) : this()
        {
            Categories = categories;
        }

        [XmlIgnore]
        public CategoryList Categories { get; set; }

        public bool Sense
        {
            get { return sense; }
            set
            {
                if (sense == value)
                    return;
                sense = value;
                if (sense)
                    OnListChanged(NotifyCollectionChangedAction.Reset);
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
            T node = new T { Name = name, Text = header };
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
            for (int i = 0; i < item.Nodes.Count; i++)
            {
                Add((T)item.Nodes[i]);
            }
            return index;
        }

        public void CheckGrop(T item)
        {
            if (item.GroupName != null && item.Group == null)
            {
                item.group = Find(item.GroupName);
            }
            if (item.CategoryName != null && item.Category == null)
            {
                item.category = Categories.Find(item.CategoryName);
            }
        }

        public override bool Remove(T item)
        {
            bool flag = base.Remove(item);
            foreach (Node n in item.Nodes)
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

        public override void OnPropertyChanged(object sender, string propertyName)
        {
            //
            if (propertyName.Equals(nameof(Node.Expand), StringComparison.Ordinal))
            {
                OnListChanged(NotifyCollectionChangedAction.Reset);
                //for (int i = 0; i < node.Childs.Count; i++)
                //{
                //    var n = node.Childs[i];
                //    this.OnNotifyPropertyChanged(n, e);
                //    base.OnNotifyPropertyChanged(n, e);
                //}
            }
            else if (propertyName.Equals(nameof(Node.Visible), StringComparison.Ordinal))
            {
                OnListChanged(NotifyCollectionChangedAction.Reset);
                //base.OnNotifyPropertyChanged(sender, e);
                //for (int i = 0; i < node.Childs.Count; i++)
                //{
                //    var n = node.Childs[i];
                //    this.OnNotifyPropertyChanged(n, e);
                //}
            }
            else if (propertyName.Equals(nameof(Node.Group), StringComparison.Ordinal))
            {
                var lindex = indexes.GetIndex(propertyName);
                if (lindex != null)
                {
                    lindex.Remove((T)sender);
                    lindex.Add((T)sender);
                }
                OnListChanged(NotifyCollectionChangedAction.Reset);
            }
            base.OnPropertyChanged(sender, propertyName);
        }

        public IEnumerable<T> GetTopLevel()
        {
            return Select(nameof(Node.Group), CompareType.Equal, null);
        }

        public void ExpandTop()
        {
            foreach (var node in GetTopLevel())
            {
                node.Expand = true;
            }
        }

        public IEnumerable<T> GetChecked()
        {
            return Select(nameof(Node.Check), CompareType.Equal, true);
        }
    }
}

