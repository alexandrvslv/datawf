using System.Xml;

namespace Doc.Odf
{
    public class TextSortKey : DocumentElement
    {
        public TextSortKey(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public string Key
        {
            get { return Element.GetAttribute("text:key"); }
            set { Service.SetAttribute(Element, "text:key", Service.nsText, value); }
        }
        //bool
        public string SortAscending
        {
            get { return Element.GetAttribute("text:sort-ascending"); }
            set { Service.SetAttribute(Element, "text:sort-ascending", Service.nsText, value); }
        }
    }

}