using System.Xml;

namespace Doc.Odf
{
    public class RowProperties : BaseProperties
    {
        public RowProperties(ODFDocument document, XmlElement element)
            : base(document, element)
        { }
        public RowProperties(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.RowProperties, Service.nsStyle))
        {
        }
        public string KeepTogether
        {
            get { return GetAttributeByParent("fo:keep-together"); }
            set { Service.SetAttribute(Element, "keep-together", Service.nsFO, value); }
        }
    }

}