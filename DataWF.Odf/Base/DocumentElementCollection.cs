using System;
using System.Collections.Generic;
using System.Xml;


namespace Doc.Odf
{
    public class DocumentElementCollection : DocumentElement, IList<BaseItem>
    {
        protected List<BaseItem> Items = new List<BaseItem>();
        public DocumentElementCollection(ODFDocument doc, XmlElement element)
            : base(doc, element)
        {
            foreach (XmlNode xn in element.ChildNodes)
                this.Items.Add(Service.Fabrica(doc, xn, this));
        }

        public BaseItem FirstElement
        {
            get { return Items.Count == 0 ? null : Items[0]; }
        }

        public BaseItem LastElement
        {
            get { return Items.Count == 0 ? null : Items[Items.Count - 1]; }
        }

        public List<BaseItem> GetChildRecursive(Type type)
        {
            return Service.GetChildsRecursive(this, type);
        }

        public List<BaseItem> GetChilds(Type type)
        {
            return Service.GetChilds(this, type);
        }

        public BaseItem this[string name]
        {
            get
            {
                foreach (DocumentElement di in Items)
                    if (di.Element.Name == name)
                        return di;
                return null;
            }
        }

        public BaseItem this[string attributeName, string value]
        {
            get
            {
                foreach (DocumentElement di in Items)
                    if (di.Element.GetAttribute(attributeName) == value)
                        return di;
                return null;
            }
        }

        #region ICollection<DocumentItem> Members

        public virtual void Add(BaseItem item)
        {
            if (Items.Contains(item))
                return;
            item.Document = this.document;
            item.Owner = this;
            if (item.Node.OwnerDocument != this.node.OwnerDocument)
                item.Node = this.node.OwnerDocument.ImportNode(item.Node, true);
            node.AppendChild(item.Node);
            Items.Add(item);
        }

        public virtual void InsertAfter(BaseItem oldItem, BaseItem newItem)
        {
            newItem.Owner = this;
            int oldIndex = Items.IndexOf(oldItem);
            if (newItem.Node.OwnerDocument != this.node.OwnerDocument)
                newItem.Node = this.node.OwnerDocument.ImportNode(newItem.Node, true);
            Element.InsertAfter(newItem.Node, oldItem.Node);
            Items.Insert(oldIndex + 1, newItem);
        }

        public virtual void InsertBefore(BaseItem oldItem, BaseItem newItem)
        {
            newItem.Owner = this;
            int oldIndex = Items.IndexOf(oldItem);
            oldIndex = oldIndex == 0 ? oldIndex + 1 : oldIndex;
            if (newItem.Node.OwnerDocument != this.node.OwnerDocument)
                newItem.Node = this.node.OwnerDocument.ImportNode(newItem.Node, true);
            Element.InsertBefore(newItem.Node, oldItem.Node);
            Items.Insert(oldIndex, newItem);
        }

        public void Replace(BaseItem oldItem, BaseItem newItem)
        {
            int oldIdex = Items.IndexOf(oldItem);
            if (oldIdex == -1)
                return;
            newItem.Owner = this;
            if (newItem.Node.OwnerDocument != this.node.OwnerDocument)
                newItem.Node = this.node.OwnerDocument.ImportNode(newItem.Node, true);
            Node.ReplaceChild(newItem.Node, oldItem.Node);
            Items[oldIdex] = newItem;
        }

        public void Clear()
        {
            foreach (BaseItem item in Items)
                node.RemoveChild(item.Node);
            Items.Clear();
        }

        public bool Contains(BaseItem item)
        {
            return Items.Contains(item);
        }

        public int IndexOf(BaseItem item)
        {
            return Items.IndexOf(item);
        }

        public void CopyTo(BaseItem[] array, int arrayIndex)
        {
            Items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return Items.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(BaseItem item)
        {
            if (!Items.Contains(item))
                return false;
            Element.RemoveChild(item.Node);
            return Items.Remove(item);
        }
        public void RemoveAt(int index)
        {
            Items.RemoveAt(index);
        }

        #endregion

        #region IEnumerable<DocumentItem> Members

        public IEnumerator<BaseItem> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        #endregion


        public void Insert(int index, BaseItem item)
        {
            if (Element.ChildNodes.Count > 0)
                Element.InsertBefore(Items[index].Node, item.Node);
            else
                Element.AppendChild(item.Node);
            Items.Insert(index, item);
        }

        public BaseItem this[int index]
        {
            get
            {
                return Items[index];
            }
            set
            {
                Element.ReplaceChild(value.Node, Items[index].Node);
                Items[index] = value;
            }
        }
    }

}
