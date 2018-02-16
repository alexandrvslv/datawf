using System.Xml;


namespace Doc.Odf
{
    public class TableOfContentEntryTemplate : DocumentElementCollection
    {
        public TableOfContentEntryTemplate(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public TextStyle Style
        {
            get { return (TextStyle)document.GetStyle(StyleName); }
            set
            {
                if (value == null)
                    return;
                StyleName = value.Name;
            }
        }
        public string StyleName
        {
            get { return Element.GetAttribute("text:style-name"); }
            set { Service.SetAttribute(Element, "text:style-name", Service.nsText, value); }
        }
        public string OutlineLevel
        {
            get { return Element.GetAttribute("text:outline-level"); }
            set { Service.SetAttribute(Element, "text:outline-level", Service.nsText, value); }
        }
    }

}
