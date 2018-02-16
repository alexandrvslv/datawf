using System.Xml;


namespace Doc.Odf
{
    public class IndexEntryTabStop : DocumentElementCollection
    {
        public IndexEntryTabStop(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public string Type
        {
            get { return Element.GetAttribute("style:type"); }
            set { Service.SetAttribute(Element, "style:type", Service.nsStyle, value); }
        }
        public string LeaderChar
        {
            get { return Element.GetAttribute("style:leader-char"); }
            set { Service.SetAttribute(Element, "style:leader-char", Service.nsStyle, value); }
        }
    }

}
