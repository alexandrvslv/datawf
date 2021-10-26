using System.Xml;


namespace Doc.Odf
{
    public class TableOfContentSource : DocumentElementCollection
    {
        public TableOfContentSource(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public string OutlineLevel
        {
            get { return Element.GetAttribute("text:outline-level"); }
            set { Service.SetAttribute(Element, "text:outline-level", Service.nsText, value); }
        }
        public string UseIndexMarks
        {
            get { return Element.GetAttribute("text:use-index-marks"); }
            set { Service.SetAttribute(Element, "text:use-index-marks", Service.nsText, value); }
        }
    }

}
