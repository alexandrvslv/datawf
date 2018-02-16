using System;
using System.Xml;


namespace Doc.Odf
{
    public class BaseItem : ICloneable
    {
        protected ODFDocument document;
        protected XmlNode node;
        protected DocumentElementCollection parentItem;

        public BaseItem(ODFDocument document, XmlNode node)
        {
            this.document = document;
            this.node = node;
        }
        public DocumentElementCollection Owner
        {
            get { return parentItem; }
            set { parentItem = value; }
        }
        public ODFDocument Document
        {
            get { return document; }
            set { document = value; }
        }
        public virtual XmlNode Node
        {
            get { return node; }
            set { node = value; }
        }
        public string XmlContent
        {
            get { return node.OuterXml; }
        }
        public bool IsFirstElement
        {
            get { return node.ParentNode == null ? false : node.ParentNode.FirstChild == node; }
        }
        public bool IsLastElement
        {
            get { return node.ParentNode == null ? false : node.ParentNode.LastChild == node; }
        }
        #region ICloneable Members

        public virtual object Clone()
        {
            return Service.Fabrica(this.document, node.Clone());
        }

        #endregion
    }

}
