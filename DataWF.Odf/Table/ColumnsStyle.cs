using System.Xml;

namespace Doc.Odf
{
    public class ColumnsStyle : DocumentElementCollection
    {
        public ColumnsStyle(ODFDocument document, XmlElement element)
            : base(document, element)
        { }
        public ColumnsStyle(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.ColumnsStyle, Service.nsStyle))
        { }
        public string ColumnsCount
        {
            get { return Element.GetAttribute("fo:column-count"); }
            set { Element.SetAttribute("fo:column-count", value); }
        }
        public string ColumnGap
        {
            get { return Element.GetAttribute("fo:column-gap"); }
            set { Element.SetAttribute("fo:column-gap", value); }
        }
    }

}