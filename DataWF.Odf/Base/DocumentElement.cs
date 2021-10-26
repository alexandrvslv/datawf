using System.Xml;


namespace Doc.Odf
{
    public class DocumentElement : BaseItem
    {
        public DocumentElement(ODFDocument document, XmlElement node)
            : base(document, node)
        {
        }
        public virtual XmlElement Element
        {
            get { return (XmlElement)node; }
            set { node = value; }
        }

    }

}
