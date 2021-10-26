using System.Xml;

namespace Doc.Odf
{
    public class BaseStyle : DocumentElementCollection
    {
        public BaseStyle(ODFDocument document, XmlElement Element)
            : base(document, Element)
        { }
        public string Name
        {
            get { return Element.GetAttribute("style:name"); }
            set { Service.SetAttribute(Element, "style:name", Service.nsStyle, value); }
        }
    }

}