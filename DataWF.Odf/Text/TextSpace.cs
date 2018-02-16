using System.Xml;


namespace Doc.Odf
{
    public class TextSpace : DocumentElement, ITextual, ITextContainer
    {
        public TextSpace(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }
        public TextSpace(ODFDocument document)
            : base(document, document.xmlContent.CreateElement(Service.TextSpace, Service.nsText))
        {
        }
        //text:s text:c="2"
        public string TextC
        {
            get { return Element.GetAttribute("text:c"); }
            set { Service.SetAttribute(Element, "text:c", Service.nsText, value); }
        }
        public int Count
        {
            get { return TextC == "" ? 1 : int.Parse(TextC); }
            set { TextC = value == 1 || value == 0 ? "" : value.ToString(); }
        }
        public string Value
        {
            get
            {
                string rez = " ";
                for (int i = 1; i < Count; i++)
                    rez += " ";
                return rez;
            }
            set { }
        }
    }

}
