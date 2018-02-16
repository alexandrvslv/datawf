using System.Xml;


namespace Doc.Odf
{
    public class FrameImage : DocumentElementCollection
    {
        public FrameImage(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public FrameImage(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.FrameImage, Service.nsDraw))
        {
            Type = "simple";
            Show = "embed";
        }
        //xlink:href="Pictures/10000000000001510000019C33FBBEAC.png" 
        //xlink:type="simple" xlink:show="embed" xlink:actuate="onLoad"
        public string HRef
        {
            get { return Element.GetAttribute("xlink:href"); }
            set { Service.SetAttribute(Element, "xlink:href", Service.nsXLink, value); }
        }
        public string Type
        {
            get { return Element.GetAttribute("xlink:type"); }
            set { Service.SetAttribute(Element, "xlink:type", Service.nsXLink, value); }
        }
        public string Show
        {
            get { return Element.GetAttribute("xlink:show"); }
            set { Service.SetAttribute(Element, "xlink:show", Service.nsXLink, value); }
        }
        public string Actuate
        {
            get { return Element.GetAttribute("xlink:actuate"); }
            set { Service.SetAttribute(Element, "xlink:actuate", Service.nsXLink, value); }
        }
        public Frame Frame
        {
            get { return (Frame)Owner; }
        }
    }

}
