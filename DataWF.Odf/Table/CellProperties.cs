using System.Xml;

namespace Doc.Odf
{
    public class CellProperties : BaseProperties
    {
        public CellProperties(ODFDocument document, XmlElement element)
            : base(document, element)
        { }
        public CellProperties(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.CellProperties, Service.nsStyle))
        {
            this.Padding = "0.097cm";
            this.BorderBottom = "0.002cm solid #000000";
            this.BorderLeft = "0.002cm solid #000000";
            this.BorderRight = "0.002cm solid #000000";
            this.BorderTop = "0.002cm solid #000000";
        }

        //fo:padding="0.097cm" fo:border-left="0.002cm solid #000000" fo:border-right="none" 
        //fo:border-top="0.002cm solid #000000" fo:border-bottom="0.002cm solid #000000"
        public string Padding
        {
            get { return GetAttributeByParent("fo:padding"); }
            set { Service.SetAttribute(Element, "fo:padding", Service.nsFO, value); }
        }
        public string BorderLeft
        {
            get { return GetAttributeByParent("fo:border-left"); }
            set { Service.SetAttribute(Element, "fo:border-left", Service.nsFO, value); }
        }
        public string BorderRight
        {
            get { return GetAttributeByParent("fo:border-right"); }
            set { Service.SetAttribute(Element, "fo:border-right", Service.nsFO, value); }
        }
        public string BorderTop
        {
            get { return GetAttributeByParent("fo:border-top"); }
            set { Service.SetAttribute(Element, "fo:border-top", Service.nsFO, value); }
        }
        public string BorderBottom
        {
            get { return GetAttributeByParent("fo:border-bottom"); }
            set { Service.SetAttribute(Element, "fo:border-bottom", Service.nsFO, value); }
        }
    }

}