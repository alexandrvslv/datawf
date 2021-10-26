using System.Xml;


namespace Doc.Odf
{
    public class IndexTitleTemplate : DocumentElementCollection
    {
        public IndexTitleTemplate(ODFDocument document, XmlElement Element)
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
    }

}
