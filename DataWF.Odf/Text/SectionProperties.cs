using System.Xml;

namespace Doc.Odf
{
    public class SectionProperties : DocumentElementCollection
    {
        public SectionProperties(ODFDocument document, XmlElement element)
            : base(document, element)
        { }
        //<style:section-properties fo:background-color="transparent" 
        //style:editable="false" 
        //<style:columns fo:column-count="1" fo:column-gap="0in" />
        //<style:background-image />
        //</style:section-properties></style:style>
        public string BackgroundColor
        {
            get { return Element.GetAttribute("fo:background-color"); }
            set { Service.SetAttribute(Element, "background-color", Service.nsFO, value); }
        }
        public string Editable
        {
            get { return Element.GetAttribute("style:editable"); }
            set { Service.SetAttribute(Element, "editable", Service.nsStyle, value); }
        }
    }

}