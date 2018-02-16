using System.Xml;


namespace Doc.Odf
{
    public class TextSequenceDeclaration : DocumentElement
    {
        //text:sequence-decl
        public TextSequenceDeclaration(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        //text:display-outline-level
        public string DisplayOutlineLevel
        {
            get { return Element.GetAttribute("text:display-outline-level"); }
            set { Service.SetAttribute(Element, "text:display-outline-level", Service.nsText, value); }
        }
        //text:name
        public string Name
        {
            get { return Element.GetAttribute("text:name"); }
            set { Service.SetAttribute(Element, "text:name", Service.nsText, value); }
        }
    }

}
