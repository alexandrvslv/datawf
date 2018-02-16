using System.Xml;


namespace Doc.Odf
{
    public class TextTab : DocumentElement, ITextual, ITextContainer
    {
        public TextTab(ODFDocument document, XmlElement Element)
            : base(document, Element)
        {
        }

        public string Value
        {
            get { return "\t"; }
            set { }
        }
    }

}
