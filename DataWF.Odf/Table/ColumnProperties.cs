using System.Xml;

namespace Doc.Odf
{
    public class ColumnProperties : BaseProperties
    {
        public ColumnProperties(ODFDocument document, XmlElement element)
            : base(document, element)
        { }
        public ColumnProperties(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.ColumnProperties, Service.nsStyle))
        { }
        //style:column-width="8.498cm" style:rel-column-width="32767*" fo:break-before="auto"
        public string BreakBefore
        {
            get { return GetAttributeByParent("fo:break-before"); }
            set { Service.SetAttribute(Element, "fo:break-before", Service.nsFO, value); }
        }
        public string Width
        {
            get { return GetAttributeByParent("style:column-width"); }
            set { Service.SetAttribute(Element, "style:column-width", Service.nsStyle, value); }
        }
        public string RelWidth
        {
            get { return GetAttributeByParent("style:rel-column-width"); }
            set { Service.SetAttribute(Element, "style:rel-column-width", Service.nsStyle, value); }
        }
    }

}