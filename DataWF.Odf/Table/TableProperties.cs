using System.Xml;

namespace Doc.Odf
{
    public class TableProperties : BaseProperties
    {
        //style:table-properties
        public TableProperties(ODFDocument document, XmlElement element)
            : base(document, element)
        { }
        public TableProperties(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.TableProperties, Service.nsStyle))
        { }
        //style:width="16.999cm" table:align="margins" table:border-model="collapsing"
        public string Width
        {
            get { return GetAttributeByParent("style:width"); }
            set { Service.SetAttribute(Element, "width", Service.nsStyle, value); }
        }
        public string Align
        {
            get { return GetAttributeByParent("table:align"); }
            set { Element.SetAttribute("align", Service.nsTable, value); }
        }
        public string BorderModel
        {
            get { return GetAttributeByParent("table:border-model"); }
            set { Service.SetAttribute(Element, "border-model", Service.nsTable, value); }
        }
    }

}